using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Backseat.Core;

namespace Backseat.Backends.Cua;

public sealed class CuaCliBackend : IComputerBackend, IRecordingBackend
{
    private readonly ICuaCli _cli;

    public CuaCliBackend(ICuaCli cli)
    {
        _cli = cli ?? throw new ArgumentNullException(nameof(cli));
    }

    public string Name => "cua-driver";

    public BackendCapabilities Capabilities =>
        BackendCapabilities.TargetDiscovery
        | BackendCapabilities.Screenshot
        | BackendCapabilities.AccessibilityTree
        | BackendCapabilities.BackgroundClick
        | BackendCapabilities.BackgroundTyping
        | BackendCapabilities.ForegroundInput
        | BackendCapabilities.Recording;

    public async Task<IReadOnlyList<TargetDescriptor>> DiscoverTargetsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _cli.CallAsync("list_windows", null, cancellationToken);

        using var json = RequireJson(result, "list_windows");

        var targets = new List<TargetDescriptor>();
        foreach (var window in json.RootElement.GetProperty("windows").EnumerateArray())
        {
            targets.Add(new TargetDescriptor
            {
                ProcessId = window.GetProperty("pid").GetUInt32(),
                WindowId = window.TryGetProperty("window_id", out var windowId) ? windowId.GetUInt64() : null,
                Title = window.TryGetProperty("title", out var title) ? title.GetString() : null,
            });
        }

        return targets;
    }

    public async Task<Observation> ObserveAsync(TargetDescriptor target, CancellationToken cancellationToken = default)
    {
        if (target.WindowId is null)
        {
            throw new ArgumentException("Observation requires an explicit window id.", nameof(target));
        }

        var arguments = new JsonObject
        {
            ["pid"] = target.ProcessId,
            ["window_id"] = target.WindowId.Value,
        };

        var result = await _cli.CallAsync("get_window_state", arguments.ToJsonString(), cancellationToken);

        using var json = RequireJson(result, "get_window_state");
        var root = json.RootElement;

        byte[]? screenshot = null;
        if (root.TryGetProperty("screenshot_png_b64", out var screenshotNode) && screenshotNode.ValueKind == JsonValueKind.String)
        {
            screenshot = Convert.FromBase64String(screenshotNode.GetString()!);
        }

        return new Observation
        {
            Target = target,
            Timestamp = DateTimeOffset.UtcNow,
            ScreenshotPng = screenshot,
            AccessibilityTree = root.TryGetProperty("tree_markdown", out var tree) && tree.ValueKind == JsonValueKind.String
                ? tree.GetString()
                : null,
            Elements = ParseElements(root),
            IsDegraded = root.TryGetProperty("degraded", out var degraded) && degraded.ValueKind == JsonValueKind.True,
            DegradedReason = GetString(root, "degraded_reason"),
        };
    }

    public Task<ActionReceipt> ExecuteAsync(TargetDescriptor target, ComputerAction action, CancellationToken cancellationToken = default)    {
        return action switch
        {
            WaitAction wait => WaitAsync(wait, cancellationToken),
            ClickAction click => ExecuteClickAsync(target, click, cancellationToken),
            TypeTextAction type => ExecuteToolAsync(target, "type_text", new JsonObject { ["text"] = type.Text }, cancellationToken),
            PressKeyAction key => ExecuteToolAsync(target, "press_key", new JsonObject { ["key"] = key.Key }, cancellationToken),
            ScrollAction scroll => ExecuteScrollAsync(target, scroll, cancellationToken),
            _ => Task.FromResult(Failed($"Unsupported action type '{action.GetType().Name}'.")),
        };
    }

    public async Task StartRecordingAsync(string outputDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Recording output directory must not be empty.", nameof(outputDirectory));
        }

        var arguments = new JsonObject
        {
            ["output_dir"] = outputDirectory,
            ["record_video"] = true,
        };

        var result = await _cli.CallAsync("start_recording", arguments.ToJsonString(), cancellationToken);

        using var json = RequireJson(result, "start_recording");

        if (!json.RootElement.TryGetProperty("enabled", out var enabled) || enabled.ValueKind != JsonValueKind.True)
        {
            throw new InvalidOperationException($"cua-driver start_recording did not enable recording: {result.StandardOutput.Trim()}");
        }
    }

    public async Task<string?> StopRecordingAsync(CancellationToken cancellationToken = default)
    {
        var result = await _cli.CallAsync("stop_recording", "{}", cancellationToken);

        using var json = RequireJson(result, "stop_recording");

        return json.RootElement.TryGetProperty("last_video_path", out var path) && path.ValueKind == JsonValueKind.String
            ? path.GetString()
            : null;
    }

    private async Task<ActionReceipt> ExecuteClickAsync(TargetDescriptor target, ClickAction click, CancellationToken cancellationToken)
    {
        var arguments = new JsonObject();

        if (click.ElementToken is not null)
        {
            arguments["element_token"] = click.ElementToken;
        }
        else
        {
            arguments["x"] = click.X;
            arguments["y"] = click.Y;
        }

        return await ExecuteToolAsync(target, "click", arguments, cancellationToken);
    }

    private async Task<ActionReceipt> ExecuteScrollAsync(TargetDescriptor target, ScrollAction scroll, CancellationToken cancellationToken)
    {
        var direction = scroll.Axis == ScrollAxis.Vertical
            ? scroll.IsForward ? "down" : "up"
            : scroll.IsForward ? "right" : "left";

        var arguments = new JsonObject
        {
            ["direction"] = direction,
            ["amount"] = Math.Clamp(Math.Abs(scroll.Ticks), 1, 50),
            ["by"] = "line",
        };

        return await ExecuteToolAsync(target, "scroll", arguments, cancellationToken);
    }

    private async Task<ActionReceipt> ExecuteToolAsync(TargetDescriptor target, string tool, JsonObject arguments, CancellationToken cancellationToken)
    {
        arguments["pid"] = target.ProcessId;
        arguments["delivery_mode"] = "background";

        var stopwatch = Stopwatch.StartNew();
        var result = await _cli.CallAsync(tool, arguments.ToJsonString(), cancellationToken);
        stopwatch.Stop();

        if (!result.Succeeded)
        {
            return Failed(
                $"cua-driver {tool} exited with code {result.ExitCode}: {FirstNonEmpty(result.StandardError, result.StandardOutput)}",
                stopwatch.Elapsed);
        }

        JsonDocument json;
        try
        {
            json = JsonDocument.Parse(result.StandardOutput);
        }
        catch (JsonException exception)
        {
            return Failed($"cua-driver {tool} returned unparseable output: {exception.Message}", stopwatch.Elapsed);
        }

        using (json)
        {
            var root = json.RootElement;

            if (root.TryGetProperty("refusal", out var refusal))
            {
                var code = refusal.TryGetProperty("code", out var refusalCode) ? refusalCode.GetString() : "unknown";
                var message = refusal.TryGetProperty("message", out var refusalMessage) ? refusalMessage.GetString() : "no message";
                return Failed($"{tool} refused: {code}: {message}", stopwatch.Elapsed);
            }

            if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
            {
                return Failed($"{tool} failed: {error.GetString()}", stopwatch.Elapsed);
            }

            return new ActionReceipt
            {
                Effect = ParseEffect(root),
                Delivery = ParseDelivery(root),
                DeliveryRoute = root.TryGetProperty("route", out var route) && route.ValueKind == JsonValueKind.String ? route.GetString() : null,
                SuggestedEscalation = ParseEscalation(root),
                Warnings = ParseWarnings(root),
                Duration = stopwatch.Elapsed,
            };
        }
    }

    private static IReadOnlyList<ObservationElement> ParseElements(JsonElement root)
    {
        if (!root.TryGetProperty("elements", out var elements) || elements.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<ObservationElement>();
        }

        var parsed = new List<ObservationElement>();
        foreach (var element in elements.EnumerateArray())
        {
            parsed.Add(new ObservationElement(
                Role: GetString(element, "role") ?? "unknown",
                Label: GetString(element, "label"),
                Value: GetString(element, "value"),
                ElementToken: GetString(element, "element_token"),
                Frame: ParseFrame(element),
                Actions: ParseStringArray(element, "actions"),
                Depth: element.TryGetProperty("depth", out var depth) && depth.TryGetInt32(out var depthValue) ? depthValue : 0,
                Enabled: !element.TryGetProperty("enabled", out var enabled) || enabled.ValueKind != JsonValueKind.False));
        }

        return parsed;
    }

    private static ObservationElementFrame? ParseFrame(JsonElement element)
    {
        if (!element.TryGetProperty("frame", out var frame) || frame.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new ObservationElementFrame(
            X: GetDouble(frame, "x"),
            Y: GetDouble(frame, "y"),
            Width: GetDouble(frame, "w"),
            Height: GetDouble(frame, "h"));
    }

    private static IReadOnlyList<string> ParseStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return array.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToList();
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static double GetDouble(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.TryGetDouble(out var number) ? number : 0d;

    private static async Task<ActionReceipt> WaitAsync(WaitAction wait, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        await Task.Delay(wait.Duration, cancellationToken);
        stopwatch.Stop();

        return new ActionReceipt
        {
            Effect = ActionEffect.Confirmed,
            DeliveryRoute = "local",
            Duration = stopwatch.Elapsed,
        };
    }

    private static JsonDocument RequireJson(CuaCommandResult result, string tool)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"cua-driver {tool} exited with code {result.ExitCode}: {FirstNonEmpty(result.StandardError, result.StandardOutput)}");
        }

        try
        {
            return JsonDocument.Parse(result.StandardOutput);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"cua-driver {tool} returned unparseable output: {exception.Message}", exception);
        }
    }

    private static ActionEffect ParseEffect(JsonElement root)
    {
        if (!root.TryGetProperty("effect", out var effect) || effect.ValueKind != JsonValueKind.String)
        {
            return ActionEffect.Unknown;
        }

        return effect.GetString() switch
        {
            "confirmed" => ActionEffect.Confirmed,
            "unverifiable" => ActionEffect.Unverifiable,
            "failed" => ActionEffect.Failed,
            _ => ActionEffect.Unknown,
        };
    }

    private static ActionDelivery ParseDelivery(JsonElement root)
    {
        if (!root.TryGetProperty("delivery", out var delivery)
            || delivery.ValueKind != JsonValueKind.Object
            || !delivery.TryGetProperty("mode", out var mode)
            || mode.ValueKind != JsonValueKind.String)
        {
            return ActionDelivery.Unknown;
        }

        return mode.GetString() switch
        {
            "background" => ActionDelivery.Background,
            "foreground" => ActionDelivery.Foreground,
            _ => ActionDelivery.Unknown,
        };
    }

    private static ActionDelivery? ParseEscalation(JsonElement root)
    {
        if (!root.TryGetProperty("escalation", out var escalation)
            || escalation.ValueKind != JsonValueKind.Object
            || !escalation.TryGetProperty("target", out var target)
            || target.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return target.GetString() switch
        {
            "background" => ActionDelivery.Background,
            "foreground" => ActionDelivery.Foreground,
            _ => null,
        };
    }

    private static IReadOnlyList<string> ParseWarnings(JsonElement root)
    {
        if (!root.TryGetProperty("escalation", out var escalation)
            || escalation.ValueKind != JsonValueKind.Object
            || !escalation.TryGetProperty("reason", out var reason)
            || reason.ValueKind != JsonValueKind.String)
        {
            return Array.Empty<string>();
        }

        return new[] { $"escalation: {reason.GetString()}" };
    }

    private static ActionReceipt Failed(string error, TimeSpan? duration = null) => new()
    {
        Effect = ActionEffect.Failed,
        Error = error,
        Duration = duration,
    };

    private static string FirstNonEmpty(string first, string second) =>
        !string.IsNullOrWhiteSpace(first) ? first.Trim() : second.Trim();
}
