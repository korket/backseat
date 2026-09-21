using System.Text.Json;
using System.Text.Json.Nodes;
using Backseat.Core;
using Backseat.Core.Runs;

namespace Backseat.Mcp;

public sealed class McpServer : IAsyncDisposable
{
    private readonly IComputerBackend _backend;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly string? _runsRoot;
    private readonly DeliveryPolicy _deliveryPolicy;

    private Session? _session;
    private RunWriter? _writer;
    private McpTools? _tools;
    private bool _initialized;

    public McpServer(
        IComputerBackend backend,
        TextReader input,
        TextWriter output,
        string? runsRoot = null,
        DeliveryPolicy deliveryPolicy = DeliveryPolicy.BackgroundOnly)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _runsRoot = runsRoot;
        _deliveryPolicy = deliveryPolicy;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await _input.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            await HandleLineAsync(line, cancellationToken);
        }

        return 0;
    }

    public async ValueTask DisposeAsync()
    {
        if (_session is not null)
        {
            await _session.CloseAsync();
        }

        if (_writer is not null)
        {
            await _writer.DisposeAsync();
        }
    }

    private async Task HandleLineAsync(string line, CancellationToken cancellationToken)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(line);
        }
        catch (JsonException)
        {
            await WriteErrorAsync(null, -32700, "Parse error");
            return;
        }

        using (document)
        {
            var root = document.RootElement;

            if (!root.TryGetProperty("method", out var methodNode) || methodNode.ValueKind != JsonValueKind.String)
            {
                await WriteErrorAsync(null, -32600, "Invalid request: missing method");
                return;
            }

            var method = methodNode.GetString()!;
            var hasId = root.TryGetProperty("id", out var idNode) && idNode.ValueKind is not JsonValueKind.Null;
            var id = hasId ? JsonNode.Parse(idNode.GetRawText()) : null;

            var parameters = root.TryGetProperty("params", out var paramsNode) && paramsNode.ValueKind == JsonValueKind.Object
                ? JsonNode.Parse(paramsNode.GetRawText())!.AsObject()
                : null;

            var response = await DispatchAsync(method, parameters, cancellationToken);

            if (!hasId)
            {
                return;
            }

            if (response.Error is not null)
            {
                await WriteAsync(new JsonObject
                {
                    ["jsonrpc"] = "2.0",
                    ["id"] = id,
                    ["error"] = response.Error,
                });
                return;
            }

            await WriteAsync(new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["result"] = response.Result,
            });
        }
    }

    private async Task<McpResponse> DispatchAsync(string method, JsonObject? parameters, CancellationToken cancellationToken)
    {
        switch (method)
        {
            case "initialize":
                _initialized = true;
                return McpResponse.Ok(new JsonObject
                {
                    ["protocolVersion"] = parameters?["protocolVersion"]?.GetValue<string>() ?? McpProtocol.DefaultProtocolVersion,
                    ["capabilities"] = new JsonObject
                    {
                        ["tools"] = new JsonObject(),
                    },
                    ["serverInfo"] = new JsonObject
                    {
                        ["name"] = "backseat",
                        ["version"] = "0.1.0",
                    },
                });

            case "notifications/initialized":
            case "notifications/cancelled":
                return McpResponse.Ok(null);

            case "ping":
                return McpResponse.Ok(new JsonObject());

            case "tools/list":
                return McpResponse.Ok(new JsonObject
                {
                    ["tools"] = McpTools.DescribeTools(),
                });

            case "tools/call":
                return await CallToolAsync(parameters, cancellationToken);

            default:
                return McpResponse.Fail(-32601, $"Method not found: {method}");
        }
    }

    private async Task<McpResponse> CallToolAsync(JsonObject? parameters, CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            return McpResponse.Fail(-32002, "Server is not initialized.");
        }

        var name = parameters?["name"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(name))
        {
            return McpResponse.Fail(-32602, "tools/call requires a tool name.");
        }

        var arguments = parameters?["arguments"] as JsonObject;

        var tools = _tools ??= new McpTools(_backend, await GetSessionAsync(cancellationToken));
        var result = await tools.CallAsync(name, arguments, cancellationToken);

        return McpResponse.Ok(result);
    }

    private async Task<Session> GetSessionAsync(CancellationToken cancellationToken)
    {
        if (_session is not null)
        {
            return _session;
        }

        var sessionId = Guid.NewGuid();

        if (_runsRoot is not null)
        {
            _writer = await RunWriter.CreateAsync(_runsRoot, sessionId, _backend.Name);
        }

        _session = new Session(_backend, _writer, sessionId, deliveryPolicy: _deliveryPolicy);
        return _session;
    }

    private async Task WriteErrorAsync(JsonNode? id, int code, string message)
    {
        await WriteAsync(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message,
            },
        });
    }

    private async Task WriteAsync(JsonObject payload)
    {
        await _output.WriteLineAsync(payload.ToJsonString(McpProtocol.SerializerOptions));
        await _output.FlushAsync();
    }
}
