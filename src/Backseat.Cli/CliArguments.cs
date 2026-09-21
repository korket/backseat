namespace Backseat.Cli;

public sealed class CliArguments
{
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _flags = new(StringComparer.OrdinalIgnoreCase);

    private CliArguments()
    {
    }

    public static CliArguments Parse(IEnumerable<string> args, IReadOnlySet<string> valueOptions, IReadOnlySet<string> flagOptions)
    {
        var arguments = new CliArguments();
        var queue = new Queue<string>(args);

        while (queue.Count > 0)
        {
            var token = queue.Dequeue();

            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                throw new CliUsageException($"Unexpected argument '{token}'.");
            }

            var name = token[2..];
            if (name.Length == 0)
            {
                throw new CliUsageException("Empty option name.");
            }

            if (flagOptions.Contains(name))
            {
                arguments._flags.Add(name);
                continue;
            }

            if (!valueOptions.Contains(name))
            {
                throw new CliUsageException($"Unknown option '--{name}'.");
            }

            if (queue.Count == 0)
            {
                throw new CliUsageException($"Option '--{name}' requires a value.");
            }

            if (!arguments._values.TryAdd(name, queue.Dequeue()))
            {
                throw new CliUsageException($"Option '--{name}' was provided more than once.");
            }
        }

        return arguments;
    }

    public bool HasFlag(string name) => _flags.Contains(name);

    public string? GetValue(string name) => _values.TryGetValue(name, out var value) ? value : null;

    public string RequireValue(string name) =>
        _values.TryGetValue(name, out var value) ? value : throw new CliUsageException($"Option '--{name}' is required.");

    public uint RequireUInt(string name)
    {
        var value = RequireValue(name);
        return uint.TryParse(value, out var parsed)
            ? parsed
            : throw new CliUsageException($"Option '--{name}' must be an unsigned integer, got '{value}'.");
    }

    public uint? GetUInt(string name)
    {
        var value = GetValue(name);
        if (value is null)
        {
            return null;
        }

        return uint.TryParse(value, out var parsed)
            ? parsed
            : throw new CliUsageException($"Option '--{name}' must be an unsigned integer, got '{value}'.");
    }
}
