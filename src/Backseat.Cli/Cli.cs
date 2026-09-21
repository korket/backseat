namespace Backseat.Cli;

public static class Cli
{
    private static readonly HashSet<string> Empty = new(StringComparer.OrdinalIgnoreCase);

    private const string Usage = """
        usage: backseat <verb> [options]

        verbs:
          targets                          List discovered targets.
          observe --pid P [--window W]     Observe the target and print elements.
          act --pid P [--window W] <action> Execute one action and print its receipt.

        options:
          --pid P            Target process id.
          --window W         Target window id (defaults to the first window of the pid).
          --runs DIR         Persist the invocation as a run under DIR.
          --json             Emit machine-readable JSON.
          --elements         List structured elements (observe).
          --screenshot PATH  Save the observation screenshot (observe).
          --save-screenshots Persist observation screenshots into the run (with --runs).

        actions:
          --click X,Y        Click window-local pixels.
          --token TOKEN      Click an element token from a previous observation.
          --type TEXT        Type text into the target.
          --key KEY          Press one key (return, escape, tab, letters, digits).
          --scroll DIR       Scroll up, down, left, or right (with --ticks N).
          --wait MS          Wait locally for MS milliseconds.

        exit codes: 0 success, 1 runtime failure or failed receipt, 2 usage error
        """;

    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0)
        {
            await error.WriteLineAsync("usage: backseat <targets|observe|act> [options]");
            return 2;
        }

        if (args[0] is "-h" or "--help" or "help")
        {
            await output.WriteLineAsync(Usage);
            return 0;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "targets" => await Commands.TargetsAsync(
                    CliArguments.Parse(args[1..], Empty, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "json" }),
                    output),

                "observe" => await Commands.ObserveAsync(
                    CliArguments.Parse(
                        args[1..],
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "pid", "window", "runs", "screenshot" },
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "json", "elements", "save-screenshots" }),
                    output),

                "act" => await Commands.ActAsync(
                    CliArguments.Parse(
                        args[1..],
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                        {
                            "pid", "window", "runs", "click", "token", "type", "key", "scroll", "ticks", "wait",
                        },
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "json", "save-screenshots" }),
                    output),

                _ => await UnknownAsync(args[0], error),
            };
        }
        catch (CliUsageException exception)
        {
            await error.WriteLineAsync(exception.Message);
            return 2;
        }
        catch (Exception exception)
        {
            await error.WriteLineAsync($"error: {exception.Message}");
            return 1;
        }
    }

    private static async Task<int> UnknownAsync(string verb, TextWriter error)
    {
        await error.WriteLineAsync($"Unknown verb '{verb}'. Use targets, observe, or act.");
        return 2;
    }
}
