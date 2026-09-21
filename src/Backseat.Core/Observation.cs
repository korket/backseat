namespace Backseat.Core;

public sealed record Observation
{
    public required TargetDescriptor Target { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public byte[]? ScreenshotPng { get; init; }

    public string? AccessibilityTree { get; init; }
}
