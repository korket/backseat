namespace Backseat.Core;

public enum ActionDelivery
{
    Unknown = 0,
    Background = 1,
    Foreground = 2,
}

public enum ActionEffect
{
    Unknown = 0,
    Confirmed = 1,
    Unverifiable = 2,
    Failed = 3,
}

public sealed record ActionReceipt
{
    public required ActionEffect Effect { get; init; }

    public ActionDelivery Delivery { get; init; } = ActionDelivery.Unknown;

    public string? DeliveryRoute { get; init; }

    public bool? ForegroundChanged { get; init; }

    public bool? CursorMoved { get; init; }

    public ActionDelivery? SuggestedEscalation { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public string? Error { get; init; }

    public TimeSpan? Duration { get; init; }

    public bool ConfirmsBackgroundSafe =>
        Delivery == ActionDelivery.Background && Effect == ActionEffect.Confirmed;
}
