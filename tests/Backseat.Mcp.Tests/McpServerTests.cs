using System.Text.Json.Nodes;
using Backseat.Core;

namespace Backseat.Mcp.Tests;

public sealed class McpServerTests
{
    private static string Line(string method, string? parameters = null, int id = 1) =>
        parameters is null
            ? $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"method\":\"{method}\"}}"
            : $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"method\":\"{method}\",\"params\":{parameters}}}";

    private static string CallTool(string name, string argumentsJson, int id) =>
        Line("tools/call", $"{{\"name\":\"{name}\",\"arguments\":{argumentsJson}}}", id);

    private static async Task<List<JsonObject>> RunAsync(
        string script,
        IComputerBackend backend,
        string? runsRoot = null,
        DeliveryPolicy policy = DeliveryPolicy.BackgroundOnly)
    {
        var output = new StringWriter();
        await using var server = new McpServer(backend, new StringReader(script), output, runsRoot, policy);
        await server.RunAsync();

        return output.ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => JsonNode.Parse(line)!.AsObject())
            .ToList();
    }

    private static JsonNode FirstTextNode(JsonObject response)
    {
        var content = response["result"]!["content"]!.AsArray();
        return JsonNode.Parse(content[0]!["text"]!.GetValue<string>())!;
    }

    private static JsonObject FirstText(JsonObject response) => FirstTextNode(response).AsObject();

    [Fact]
    public async Task Initialize_Returns_Protocol_Version_And_Server_Info()
    {
        var responses = await RunAsync(
            Line("initialize", "{\"protocolVersion\":\"2025-06-18\"}"),
            new FakeBackend());

        var result = responses[0]["result"]!.AsObject();
        Assert.Equal("2025-06-18", result["protocolVersion"]!.GetValue<string>());
        Assert.Equal("backseat", result["serverInfo"]!["name"]!.GetValue<string>());
        Assert.NotNull(result["capabilities"]!["tools"]);
    }

    [Fact]
    public async Task Initialize_Defaults_The_Protocol_Version()
    {
        var responses = await RunAsync(Line("initialize"), new FakeBackend());

        Assert.Equal("2024-11-05", responses[0]["result"]!["protocolVersion"]!.GetValue<string>());
    }

    [Fact]
    public async Task Tools_List_Advertises_The_Three_Tools()
    {
        var script = string.Join('\n', Line("initialize"), Line("tools/list", null, 2));
        var responses = await RunAsync(script, new FakeBackend());

        var tools = responses[1]["result"]!["tools"]!.AsArray();
        var names = tools.Select(tool => tool!["name"]!.GetValue<string>()).ToList();

        Assert.Equal(3, tools.Count);
        Assert.Contains("backseat_targets", names);
        Assert.Contains("backseat_observe", names);
        Assert.Contains("backseat_act", names);
        Assert.NotNull(tools[0]!["inputSchema"]);
    }

    [Fact]
    public async Task Targets_Tool_Returns_Discovered_Targets()
    {
        var script = string.Join('\n', Line("initialize"), CallTool("backseat_targets", "{}", 2));
        var responses = await RunAsync(script, new FakeBackend());

        var payload = FirstTextNode(responses[1]).AsArray();
        Assert.Equal(42u, payload[0]!["processId"]!.GetValue<uint>());
        Assert.Equal(99ul, payload[0]!["windowId"]!.GetValue<ulong>());
    }

    [Fact]
    public async Task Observe_Tool_Returns_Elements_And_Tree()
    {
        var script = string.Join('\n', Line("initialize"), CallTool("backseat_observe", "{\"pid\":42}", 2));
        var responses = await RunAsync(script, new FakeBackend());

        var payload = FirstText(responses[1]);
        Assert.Equal("s1:31", payload["elements"]![0]!["token"]!.GetValue<string>());
        Assert.Equal("Six", payload["elements"]![0]!["label"]!.GetValue<string>());
        Assert.Contains("Untitled - Notepad", payload["tree"]!.GetValue<string>());
        Assert.False(payload["degraded"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Act_Tool_Returns_A_Receipt()
    {
        var script = string.Join('\n', Line("initialize"), CallTool("backseat_act", "{\"pid\":42,\"type\":\"token\",\"token\":\"s1:31\"}", 2));
        var responses = await RunAsync(script, new FakeBackend());

        var receipt = FirstText(responses[1]);
        Assert.Equal("Confirmed", receipt["effect"]!.GetValue<string>());
        Assert.Equal("Background", receipt["delivery"]!.GetValue<string>());
        Assert.True(receipt["backgroundSafe"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Second_Target_On_The_Same_Connection_Is_Refused()
    {
        var backend = new FakeBackend();
        backend.Targets.Add(new TargetDescriptor { ProcessId = 7, WindowId = 8, Title = "Other" });

        var script = string.Join(
            '\n',
            Line("initialize"),
            CallTool("backseat_act", "{\"pid\":42,\"type\":\"wait\",\"ms\":1}", 2),
            CallTool("backseat_act", "{\"pid\":7,\"type\":\"wait\",\"ms\":1}", 3));

        var responses = await RunAsync(script, backend);

        var refusal = responses[2]["result"]!.AsObject();
        Assert.True(refusal["isError"]!.GetValue<bool>());
        Assert.Contains("bound to pid 42", refusal["content"]![0]!["text"]!.GetValue<string>());
    }

    [Fact]
    public async Task Delivery_Policy_Violations_Surface_As_Errors()
    {
        var backend = new FakeBackend
        {
            ExecuteHandler = (_, _) => Task.FromResult(new ActionReceipt
            {
                Effect = ActionEffect.Confirmed,
                Delivery = ActionDelivery.Foreground,
                DeliveryRoute = "global_input",
            }),
        };

        var script = string.Join('\n', Line("initialize"), CallTool("backseat_act", "{\"pid\":42,\"type\":\"wait\",\"ms\":1}", 2));
        var responses = await RunAsync(script, backend);

        var result = responses[1]["result"]!.AsObject();
        Assert.True(result["isError"]!.GetValue<bool>());
        Assert.Contains("delivery policy violation", result["content"]![0]!["text"]!.GetValue<string>());
    }

    [Fact]
    public async Task Unknown_Method_Returns_Method_Not_Found()
    {
        var responses = await RunAsync(Line("does/not/exist"), new FakeBackend());

        Assert.Equal(-32601, responses[0]["error"]!["code"]!.GetValue<int>());
    }

    [Fact]
    public async Task Malformed_Json_Returns_Parse_Error()
    {
        var responses = await RunAsync("{ not json", new FakeBackend());

        Assert.Equal(-32700, responses[0]["error"]!["code"]!.GetValue<int>());
    }

    [Fact]
    public async Task Tools_Before_Initialize_Are_Rejected()
    {
        var responses = await RunAsync(CallTool("backseat_targets", "{}", 1), new FakeBackend());

        Assert.Equal(-32002, responses[0]["error"]!["code"]!.GetValue<int>());
    }

    [Fact]
    public async Task Closing_The_Connection_Finalizes_The_Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "backseat-mcp-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var script = string.Join('\n', Line("initialize"), CallTool("backseat_act", "{\"pid\":42,\"type\":\"wait\",\"ms\":1}", 2));

            await RunAsync(script, new FakeBackend(), root);

            var runDirectory = Assert.Single(Directory.GetDirectories(root));
            var metadata = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(runDirectory, "metadata.json")))!.AsObject();

            Assert.Equal("Closed", metadata["finalState"]!.GetValue<string>());
            Assert.Equal(1, metadata["actionCount"]!.GetValue<int>());
            Assert.True(File.Exists(Path.Combine(runDirectory, "actions.jsonl")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Notifications_Do_Not_Produce_Responses()
    {
        var responses = await RunAsync("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}", new FakeBackend());

        Assert.Empty(responses);
    }
}
