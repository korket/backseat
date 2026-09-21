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

public sealed record ScrollAction(double DeltaX, double DeltaY) : ComputerAction;

public sealed record WaitAction(TimeSpan Duration) : ComputerAction
{
    public TimeSpan Duration { get; init; } = Duration > TimeSpan.Zero
        ? Duration
        : throw new ArgumentOutOfRangeException(nameof(Duration), Duration, "Wait duration must be positive.");
}
