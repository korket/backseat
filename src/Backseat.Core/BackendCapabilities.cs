namespace Backseat.Core;

[Flags]
public enum BackendCapabilities
{
    None = 0,
    TargetDiscovery = 1 << 0,
    Screenshot = 1 << 1,
    AccessibilityTree = 1 << 2,
    BackgroundClick = 1 << 3,
    BackgroundTyping = 1 << 4,
    ForegroundInput = 1 << 5,
    Recording = 1 << 6,
}
