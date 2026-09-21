using System.Text.Json;
using System.Text.Json.Nodes;

namespace Backseat.Mcp;

public sealed record McpResponse(JsonNode? Result, JsonObject? Error)
{
    public static McpResponse Ok(JsonNode? result) => new(result, null);

    public static McpResponse Fail(int code, string message) => new(null, new JsonObject
    {
        ["code"] = code,
        ["message"] = message,
    });
}

public sealed class McpProtocol
{
    public const string DefaultProtocolVersion = "2024-11-05";

    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
    };

    public static JsonObject TextResult(string text, bool isError = false)
    {
        var content = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "text",
                ["text"] = text,
            },
        };

        var result = new JsonObject
        {
            ["content"] = content,
        };

        if (isError)
        {
            result["isError"] = true;
        }

        return result;
    }

    public static JsonObject ImageResult(string base64Png, string mimeType = "image/png")
    {
        var content = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "image",
                ["data"] = base64Png,
                ["mimeType"] = mimeType,
            },
        };

        return new JsonObject
        {
            ["content"] = content,
        };
    }
}
