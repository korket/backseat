using Backseat.Core;

namespace Backseat.Mcp.Tests;

internal sealed class FakeBackend : IComputerBackend
{
    public List<TargetDescriptor> Targets { get; } = new()
    {
        new TargetDescriptor { ProcessId = 42, WindowId = 99, Title = "Untitled - Notepad" },
    };

    public Func<ComputerAction, CancellationToken, Task<ActionReceipt>> ExecuteHandler { get; set; } =
        (_, _) => Task.FromResult(new ActionReceipt
        {
            Effect = ActionEffect.Confirmed,
            Delivery = ActionDelivery.Background,
            DeliveryRoute = "accessibility",
        });

    public string Name => "fake";

    public BackendCapabilities Capabilities => BackendCapabilities.TargetDiscovery;

    public Task<IReadOnlyList<TargetDescriptor>> DiscoverTargetsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TargetDescriptor>>(Targets.ToList());

    public Task<Observation> ObserveAsync(TargetDescriptor target, CancellationToken cancellationToken = default) =>
        Task.FromResult(new Observation
        {
            Target = target,
            Timestamp = DateTimeOffset.UtcNow,
            AccessibilityTree = "- Window \"Untitled - Notepad\"",
            Elements = new[]
            {
                new ObservationElement("Button", "Six", null, "s1:31", null, new[] { "invoke" }, 5, true),
            },
        });

    public Task<ActionReceipt> ExecuteAsync(TargetDescriptor target, ComputerAction action, CancellationToken cancellationToken = default) =>
        ExecuteHandler(action, cancellationToken);
}
