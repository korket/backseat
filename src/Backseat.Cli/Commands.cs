using System.Text.Json;
using Backseat.Backends.Cua;
using Backseat.Core;
using Backseat.Core.Runs;

namespace Backseat.Cli;

public static class Commands
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task<int> TargetsAsync(CliArguments arguments, TextWriter output)
    {
        var backend = new CuaCliBackend(new CuaProcessRunner());
        var targets = await backend.DiscoverTargetsAsync();

        if (arguments.HasFlag("json"))
        {
            await output.WriteLineAsync(JsonSerializer.Serialize(
                targets.Select(target => new { processId = target.ProcessId, windowId = target.WindowId, title = target.Title }),
                JsonOptions));
            return 0;
        }

        foreach (var target in targets)
        {
            await output.WriteLineAsync(FormatTarget(target));
        }

        return 0;
    }

    public static Task<int> ObserveAsync(CliArguments arguments, TextWriter output) =>
        WithSessionAsync(arguments, async (session, args, _) =>
        {
            var observation = await session.ObserveAsync();

            if (args.GetValue("screenshot") is { } screenshotPath && observation.ScreenshotPng is not null)
            {
                await File.WriteAllBytesAsync(screenshotPath, observation.ScreenshotPng);
            }

            if (args.HasFlag("json"))
            {
                await output.WriteLineAsync(JsonSerializer.Serialize(DescribeObservation(observation, args.GetValue("screenshot")), JsonOptions));
                return 0;
            }

            await output.WriteLineAsync(FormatTarget(observation.Target));
            await WriteElementsAsync(observation, args, output);
            await output.WriteLineAsync(
                $"tree: {observation.AccessibilityTree?.Length ?? 0} chars, screenshot: {observation.ScreenshotPng?.Length ?? 0} bytes");
            return 0;
        });

    public static Task<int> ActAsync(CliArguments arguments, TextWriter output) =>
        WithSessionAsync(arguments, async (session, args, _) =>
        {
            var action = ActionParser.Parse(args);
            var receipt = await session.ExecuteAsync(action);

            if (args.HasFlag("json"))
            {
                await output.WriteLineAsync(JsonSerializer.Serialize(DescribeReceipt(receipt), JsonOptions));
            }
            else
            {
                await output.WriteLineAsync(
                    $"effect={receipt.Effect} delivery={receipt.Delivery} route={receipt.DeliveryRoute ?? "-"} "
                    + $"backgroundSafe={receipt.ConfirmsBackgroundSafe} error={receipt.Error ?? "-"}");
            }

            return receipt.Effect == ActionEffect.Failed ? 1 : 0;
        });

    private static async Task<int> WithSessionAsync(CliArguments arguments, Func<Session, CliArguments, TextWriter, Task<int>> run)
    {
        var backend = new CuaCliBackend(new CuaProcessRunner());
        var sessionId = Guid.NewGuid();
        var runRoot = arguments.GetValue("runs");

        RunWriter? writer = null;
        try
        {
            if (runRoot is not null)
            {
                writer = await RunWriter.CreateAsync(runRoot, sessionId, backend.Name, arguments.HasFlag("save-screenshots"));
            }

            await using var session = new Session(backend, writer, sessionId);
            var target = await ResolveTargetAsync(backend, arguments);
            await session.SelectTargetAsync(target);

            return await run(session, arguments, TextWriter.Null);
        }
        finally
        {
            if (writer is not null)
            {
                await writer.DisposeAsync();
            }
        }
    }

    private static async Task<TargetDescriptor> ResolveTargetAsync(IComputerBackend backend, CliArguments arguments)
    {
        var processId = arguments.RequireUInt("pid");
        var windowId = arguments.GetUInt("window");

        var targets = await backend.DiscoverTargetsAsync();
        var candidates = targets.Where(target => target.ProcessId == processId).ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException($"No window reported for pid {processId}.");
        }

        if (windowId is not null)
        {
            return candidates.FirstOrDefault(target => target.WindowId == windowId)
                ?? throw new InvalidOperationException($"pid {processId} has no window {windowId}.");
        }

        return candidates.FirstOrDefault(target => target.WindowId is not null) ?? candidates[0];
    }

    private static async Task WriteElementsAsync(Observation observation, CliArguments arguments, TextWriter output)
    {
        if (!arguments.HasFlag("elements"))
        {
            await output.WriteLineAsync($"elements: {observation.Elements.Count} (pass --elements to list them)");
            return;
        }

        for (var index = 0; index < observation.Elements.Count; index++)
        {
            var element = observation.Elements[index];
            var frame = element.Frame is null
                ? "-"
                : string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"{element.Frame.X},{element.Frame.Y} {element.Frame.Width}x{element.Frame.Height}");
            var actions = element.Actions.Count == 0 ? "-" : string.Join(",", element.Actions);

            await output.WriteLineAsync(
                $"[{index}] {element.Role} '{element.Label ?? string.Empty}' token={element.ElementToken ?? "-"} "
                + $"frame={frame} depth={element.Depth} enabled={element.Enabled} actions={actions}");
        }
    }

    private static string FormatTarget(TargetDescriptor target) =>
        $"{target.ProcessId}  {target.WindowId?.ToString() ?? "-"}  {target.Title ?? string.Empty}";

    private static object DescribeObservation(Observation observation, string? screenshotPath) => new
    {
        target = new { processId = observation.Target.ProcessId, windowId = observation.Target.WindowId, title = observation.Target.Title },
        timestamp = observation.Timestamp,
        screenshotPath,
        tree = observation.AccessibilityTree,
        elements = observation.Elements.Select(element => new
        {
            role = element.Role,
            label = element.Label,
            value = element.Value,
            token = element.ElementToken,
            frame = element.Frame is null
                ? null
                : new { x = element.Frame.X, y = element.Frame.Y, w = element.Frame.Width, h = element.Frame.Height },
            actions = element.Actions,
            depth = element.Depth,
            enabled = element.Enabled,
        }),
    };

    private static object DescribeReceipt(ActionReceipt receipt) => new
    {
        effect = receipt.Effect.ToString(),
        delivery = receipt.Delivery.ToString(),
        route = receipt.DeliveryRoute,
        foregroundChanged = receipt.ForegroundChanged,
        cursorMoved = receipt.CursorMoved,
        suggestedEscalation = receipt.SuggestedEscalation?.ToString(),
        warnings = receipt.Warnings,
        error = receipt.Error,
        backgroundSafe = receipt.ConfirmsBackgroundSafe,
        durationMs = receipt.Duration?.TotalMilliseconds,
    };
}
