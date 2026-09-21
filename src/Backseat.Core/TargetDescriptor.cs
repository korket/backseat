namespace Backseat.Core;

public sealed record TargetDescriptor
{
    private readonly uint _processId;

    public required uint ProcessId
    {
        get => _processId;
        init => _processId = value != 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Target identity requires an explicit process id.");
    }

    public ulong? WindowId { get; init; }

    public string? ExecutablePath { get; init; }

    public string? Title { get; init; }
}
