using System.Text.Json;
using System.Text.Json.Nodes;
using Backseat.Core;

namespace Backseat.Mcp;

public sealed class McpTools
{
    public const int MaxBatchSize = 50;

    private readonly IComputerBackend _backend;
    private readonly Session _session;
    private readonly Func<Session, string, JsonObject, CancellationToken, Task<JsonObject>> _actHandler;

    public McpTools(IComputerBackend backend, Session session)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _actHandler = ExecuteActionAsync;
    }

    public static JsonArray DescribeTools() => new()
    {
        new JsonObject
        {
            ["name"] = "targets",
            ["description"] = "List desktop targets reported by the Backseat backend (process id, window id, title).",
            ["inputSchema"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject(),
            },
        },
        new JsonObject
        {
            ["name"] = "observe",
            ["description"] = "Observe the selected target and return its structured elements, accessibility tree, and optionally a screenshot.",
            ["inputSchema"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["pid"] = new JsonObject { ["type"] = "integer", ["description"] = "Target process id." },
                    ["windowId"] = new JsonObject { ["type"] = "integer", ["description"] = "Target window id (optional)." },
                    ["includeScreenshot"] = new JsonObject { ["type"] = "boolean", ["description"] = "Include the PNG screenshot as an image block." },
                },
                ["required"] = new JsonArray("pid"),
            },
        },
        new JsonObject
        {
            ["name"] = "act",
            ["description"] = "Execute one background action, or a small sequential batch, against the selected target and return the receipt(s). Types: click, token, type, key, scroll, wait.",
            ["inputSchema"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["pid"] = new JsonObject { ["type"] = "integer", ["description"] = "Target process id." },
                    ["windowId"] = new JsonObject { ["type"] = "integer", ["description"] = "Target window id (optional)." },
                    ["type"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("click", "token", "type", "key", "scroll", "wait") },
                    ["x"] = new JsonObject { ["type"] = "number", ["description"] = "Window-local x for click." },
                    ["y"] = new JsonObject { ["type"] = "number", ["description"] = "Window-local y for click." },
                    ["token"] = new JsonObject { ["type"] = "string", ["description"] = "Element token for token clicks." },
                    ["text"] = new JsonObject { ["type"] = "string", ["description"] = "Text for type." },
                    ["key"] = new JsonObject { ["type"] = "string", ["description"] = "Key name for key presses." },
                    ["direction"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("up", "down", "left", "right") },
                    ["ticks"] = new JsonObject { ["type"] = "integer", ["description"] = "Scroll ticks (default 1)." },
                    ["ms"] = new JsonObject { ["type"] = "number", ["description"] = "Wait duration in milliseconds." },
                    ["actions"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["description"] = $"Sequential batch of up to {MaxBatchSize} action objects, each shaped like the single-action arguments. Use one of 'type' or 'actions'.",
                        ["items"] = new JsonObject { ["type"] = "object" },
                    },
                },
                ["required"] = new JsonArray("pid"),
            },
        },
    };

    public async Task<JsonObject> CallAsync(string name, JsonObject? arguments, CancellationToken cancellationToken)
    {
        try
        {
            return name switch
            {
                "targets" => await TargetsAsync(cancellationToken),
                "observe" => await ObserveAsync(arguments ?? new JsonObject(), cancellationToken),
                "act" => await ActAsync(arguments ?? new JsonObject(), cancellationToken),
                _ => McpProtocol.TextResult($"Unknown tool '{name}'.", isError: true),
            };
        }
        catch (DeliveryPolicyViolationException exception)
        {
            return McpProtocol.TextResult(
                $"delivery policy violation: {exception.Message} receipt={JsonSerializer.Serialize(DescribeReceipt(exception.Receipt))}",
                isError: true);
        }
        catch (Exception exception)
        {
            return McpProtocol.TextResult($"error: {exception.Message}", isError: true);
        }
    }

    private async Task<JsonObject> TargetsAsync(CancellationToken cancellationToken)
    {
        var targets = await _backend.DiscoverTargetsAsync(cancellationToken);
        var payload = new JsonArray(targets
            .Select(target => (JsonNode)new JsonObject
            {
                ["processId"] = target.ProcessId,
                ["windowId"] = target.WindowId,
                ["title"] = target.Title,
            })
            .ToArray());

        return McpProtocol.TextResult(payload.ToJsonString());
    }

    private async Task<JsonObject> ObserveAsync(JsonObject arguments, CancellationToken cancellationToken)
    {
        await EnsureTargetAsync(arguments, cancellationToken);

        var observation = await _session.ObserveAsync(cancellationToken);

        var payload = new JsonObject
        {
            ["target"] = new JsonObject
            {
                ["processId"] = observation.Target.ProcessId,
                ["windowId"] = observation.Target.WindowId,
                ["title"] = observation.Target.Title,
            },
            ["timestamp"] = observation.Timestamp.ToString("O"),
            ["degraded"] = observation.IsDegraded,
            ["degradedReason"] = observation.DegradedReason,
            ["tree"] = observation.AccessibilityTree,
            ["elements"] = new JsonArray(observation.Elements
                .Select(element => (JsonNode)new JsonObject
                {
                    ["role"] = element.Role,
                    ["label"] = element.Label,
                    ["value"] = element.Value,
                    ["token"] = element.ElementToken,
                    ["depth"] = element.Depth,
                    ["enabled"] = element.Enabled,
                    ["actions"] = new JsonArray(element.Actions.Select(action => (JsonNode)action!).ToArray()),
                })
                .ToArray()),
        };

        if (GetBool(arguments, "includeScreenshot") && observation.ScreenshotPng is not null)
        {
            var image = McpProtocol.ImageResult(Convert.ToBase64String(observation.ScreenshotPng));
            ((JsonArray)image["content"]!).Add(new JsonObject
            {
                ["type"] = "text",
                ["text"] = payload.ToJsonString(),
            });

            return image;
        }

        return McpProtocol.TextResult(payload.ToJsonString());
    }

    private async Task<JsonObject> ActAsync(JsonObject arguments, CancellationToken cancellationToken)
    {
        await EnsureTargetAsync(arguments, cancellationToken);

        if (arguments["actions"] is JsonArray batch)
        {
            if (batch.Count == 0)
            {
                throw new ArgumentException("The 'actions' array must not be empty.");
            }

            if (batch.Count > MaxBatchSize)
            {
                throw new ArgumentException($"A batch may contain at most {MaxBatchSize} actions; got {batch.Count}.");
            }

            var receipts = new JsonArray();
            var anyFailed = false;

            foreach (var item in batch)
            {
                var step = item as JsonObject ?? throw new ArgumentException("Each batch entry must be an object.");
                var stepReceipt = await _actHandler(_session, RequireString(step, "type"), step, cancellationToken);
                receipts.Add(stepReceipt);

                if (stepReceipt["effect"]?.GetValue<string>() == "Failed")
                {
                    anyFailed = true;
                }
            }

            return McpProtocol.TextResult(receipts.ToJsonString(), isError: anyFailed);
        }

        var receipt = await _actHandler(_session, RequireString(arguments, "type"), arguments, cancellationToken);

        return McpProtocol.TextResult(receipt.ToJsonString());
    }

    private async Task<JsonObject> ExecuteActionAsync(Session session, string type, JsonObject arguments, CancellationToken cancellationToken)
    {
        ComputerAction action = type switch
        {
            "click" => new ClickAction(RequireDouble(arguments, "x"), RequireDouble(arguments, "y")),
            "token" => new ClickAction(RequireString(arguments, "token")),
            "type" => new TypeTextAction(RequireString(arguments, "text")),
            "key" => new PressKeyAction(RequireString(arguments, "key")),
            "scroll" => BuildScroll(arguments),
            "wait" => new WaitAction(TimeSpan.FromMilliseconds(RequireDouble(arguments, "ms"))),
            _ => throw new ArgumentException($"Unknown action type '{type}'. Use click, token, type, key, scroll, or wait."),
        };

        var receipt = await session.ExecuteAsync(action, cancellationToken);
        return DescribeReceipt(receipt);
    }

    private static ScrollAction BuildScroll(JsonObject arguments)
    {
        var direction = RequireString(arguments, "direction").ToLowerInvariant();
        var ticks = GetInt(arguments, "ticks") ?? 1;

        return direction switch
        {
            "up" => new ScrollAction(ScrollAxis.Vertical, -ticks),
            "down" => new ScrollAction(ScrollAxis.Vertical, ticks),
            "left" => new ScrollAction(ScrollAxis.Horizontal, -ticks),
            "right" => new ScrollAction(ScrollAxis.Horizontal, ticks),
            _ => throw new ArgumentException($"Unknown scroll direction '{direction}'."),
        };
    }

    private async Task EnsureTargetAsync(JsonObject arguments, CancellationToken cancellationToken)
    {
        var processId = (uint)RequireDouble(arguments, "pid");
        var windowId = GetDouble(arguments, "windowId") is { } window ? (ulong)window : (ulong?)null;

        if (_session.Target is not null)
        {
            var selected = _session.Target;
            if (selected.ProcessId != processId || (windowId is not null && selected.WindowId != windowId))
            {
                throw new InvalidOperationException(
                    $"This connection is bound to pid {selected.ProcessId} window {selected.WindowId?.ToString() ?? "(none)"}. "
                    + "One MCP connection is one Backseat session; start a new connection for a different target.");
            }

            return;
        }

        var targets = await _backend.DiscoverTargetsAsync(cancellationToken);
        var candidates = targets.Where(target => target.ProcessId == processId).ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException($"No window reported for pid {processId}.");
        }

        var target = windowId is null
            ? candidates.FirstOrDefault(candidate => candidate.WindowId is not null) ?? candidates[0]
            : candidates.FirstOrDefault(candidate => candidate.WindowId == windowId)
                ?? throw new InvalidOperationException($"pid {processId} has no window {windowId}.");

        await _session.SelectTargetAsync(target, cancellationToken);
    }

    private static JsonObject DescribeReceipt(ActionReceipt receipt) => new()
    {
        ["effect"] = receipt.Effect.ToString(),
        ["delivery"] = receipt.Delivery.ToString(),
        ["route"] = receipt.DeliveryRoute,
        ["backgroundSafe"] = receipt.ConfirmsBackgroundSafe,
        ["foregroundChanged"] = receipt.ForegroundChanged,
        ["cursorMoved"] = receipt.CursorMoved,
        ["suggestedEscalation"] = receipt.SuggestedEscalation?.ToString(),
        ["warnings"] = new JsonArray(receipt.Warnings.Select(warning => (JsonNode)warning!).ToArray()),
        ["error"] = receipt.Error,
        ["durationMs"] = receipt.Duration?.TotalMilliseconds,
    };

    private static string RequireString(JsonObject arguments, string name) =>
        arguments[name]?.GetValue<string>() ?? throw new ArgumentException($"Missing required argument '{name}'.");

    private static double RequireDouble(JsonObject arguments, string name) =>
        arguments[name]?.GetValue<double>() ?? throw new ArgumentException($"Missing required argument '{name}'.");

    private static double? GetDouble(JsonObject arguments, string name) =>
        arguments[name] is { } node ? node.GetValue<double>() : null;

    private static int? GetInt(JsonObject arguments, string name) =>
        arguments[name] is { } node ? node.GetValue<int>() : null;

    private static bool GetBool(JsonObject arguments, string name) =>
        arguments[name] is { } node && node.GetValue<bool>();
}
