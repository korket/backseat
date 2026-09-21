namespace Backseat.Core;

public sealed record ObservationSettlePolicy
{
    public static readonly ObservationSettlePolicy Default = new();

    public int MaxAttempts { get; init; } = 3;

    public TimeSpan Delay { get; init; } = TimeSpan.FromMilliseconds(250);

    public ObservationSettlePolicy()
    {
    }

    public void Validate()
    {
        if (MaxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxAttempts), MaxAttempts, "At least one observation attempt is required.");
        }

        if (Delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(Delay), Delay, "Settle delay must not be negative.");
        }
    }
}
