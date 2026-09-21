namespace Backseat.Core;

public sealed record ActionRecord(
    int Sequence,
    ComputerAction Action,
    ActionReceipt Receipt,
    DateTimeOffset Timestamp);
