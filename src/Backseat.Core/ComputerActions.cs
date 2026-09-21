namespace Backseat.Core;

public abstract record ComputerAction;

public sealed record ClickAction : ComputerAction
{
    public ClickAction(double x, double y)
    {
        X = x;
        Y = y;
    }

    public ClickAction(string elementToken)
    {
        if (string.IsNullOrWhiteSpace(elementToken))
        {
            throw new ArgumentException("Element token must not be empty.", nameof(elementToken));
        }

        ElementToken = elementToken;
    }

    public double? X { get; }

    public double? Y { get; }

    public string? ElementToken { get; }

    public bool RequireBackgroundSafe { get; init; } = true;
}

public sealed record TypeTextAction(string Text) : ComputerAction
{
    public string Text { get; init; } = !string.IsNullOrEmpty(Text)
        ? Text
        : throw new ArgumentException("Text must not be empty.", nameof(Text));
}

public sealed record PressKeyAction(string Key) : ComputerAction
{
    public string Key { get; init; } = !string.IsNullOrWhiteSpace(Key)
        ? Key
        : throw new ArgumentException("Key must not be empty.", nameof(Key));
}

public enum ScrollAxis
{
    Vertical = 0,
    Horizontal = 1,
}

public sealed record ScrollAction(ScrollAxis Axis, int Ticks) : ComputerAction
{
    public int Ticks { get; init; } = Ticks != 0
        ? Ticks
        : throw new ArgumentOutOfRangeException(nameof(Ticks), Ticks, "Scroll ticks must be non-zero.");

    public bool IsForward => Ticks > 0;
}

public sealed record WaitAction(TimeSpan Duration) : ComputerAction
{
    public TimeSpan Duration { get; init; } = Duration > TimeSpan.Zero
        ? Duration
        : throw new ArgumentOutOfRangeException(nameof(Duration), Duration, "Wait duration must be positive.");
}
