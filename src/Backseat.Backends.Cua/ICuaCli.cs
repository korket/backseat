namespace Backseat.Backends.Cua;

public sealed record CuaCommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}

public interface ICuaCli
{
    Task<CuaCommandResult> CallAsync(string tool, string? jsonArguments, CancellationToken cancellationToken = default);
}
