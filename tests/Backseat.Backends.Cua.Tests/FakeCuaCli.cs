using System.Text.Json;
using Backseat.Backends.Cua;

namespace Backseat.Backends.Cua.Tests;

internal sealed class FakeCuaCli : ICuaCli
{
    private readonly Queue<CuaCommandResult> _responses = new();

    public List<(string Tool, string? Arguments)> Calls { get; } = new();

    public FakeCuaCli Enqueue(int exitCode, string standardOutput, string standardError = "")
    {
        _responses.Enqueue(new CuaCommandResult(exitCode, standardOutput, standardError));
        return this;
    }

    public Task<CuaCommandResult> CallAsync(string tool, string? jsonArguments, CancellationToken cancellationToken = default)
    {
        Calls.Add((tool, jsonArguments));

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException($"No queued response for '{tool}'.");
        }

        return Task.FromResult(_responses.Dequeue());
    }

    public JsonElement ParseArguments(int index)
    {
        var arguments = Calls[index].Arguments
            ?? throw new InvalidOperationException($"Call {index} had no arguments.");

        return JsonDocument.Parse(arguments).RootElement;
    }
}
