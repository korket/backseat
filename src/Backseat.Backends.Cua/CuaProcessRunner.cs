using System.Diagnostics;

namespace Backseat.Backends.Cua;

public sealed class CuaProcessRunner : ICuaCli
{
    private readonly string _executablePath;

    public CuaProcessRunner(string executablePath = "cua-driver")
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ArgumentException("Executable path must not be empty.", nameof(executablePath));
        }

        _executablePath = executablePath;
    }

    public async Task<CuaCommandResult> CallAsync(string tool, string? jsonArguments, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tool))
        {
            throw new ArgumentException("Tool name must not be empty.", nameof(tool));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _executablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = jsonArguments is not null,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add("call");
        startInfo.ArgumentList.Add(tool);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{_executablePath}'.");

        if (jsonArguments is not null)
        {
            await process.StandardInput.WriteAsync(jsonArguments);
            process.StandardInput.Close();
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return new CuaCommandResult(process.ExitCode, await stdoutTask, await stderrTask);
    }
}
