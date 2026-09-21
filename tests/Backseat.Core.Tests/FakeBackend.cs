using Backseat.Core;

namespace Backseat.Core.Tests;

internal sealed class FakeBackend : IComputerBackend, IAsyncDisposable
{
    public List<TargetDescriptor> Targets { get; } = new();

    public List<ComputerAction> ExecutedActions { get; } = new();

    public Func<ComputerAction, CancellationToken, Task<ActionReceipt>> ExecuteHandler { get; set; } =
        (_, _) => Task.FromResult(new ActionReceipt
        {
            Effect = ActionEffect.Confirmed,
            Delivery = ActionDelivery.Background,
            DeliveryRoute = "accessibility",
        });

    public bool Disposed { get; private set; }

    public string Name => "fake";

    public BackendCapabilities Capabilities => BackendCapabilities.TargetDiscovery;

    public Task<IReadOnlyList<TargetDescriptor>> DiscoverTargetsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TargetDescriptor>>(Targets.ToList());

    public Task<Observation> ObserveAsync(TargetDescriptor target, CancellationToken cancellationToken = default) =>
        Task.FromResult(new Observation
        {
            Target = target,
            Timestamp = DateTimeOffset.UtcNow,
            AccessibilityTree = "- Window",
        });

    public Task<ActionReceipt> ExecuteAsync(TargetDescriptor target, ComputerAction action, CancellationToken cancellationToken = default)
    {
        ExecutedActions.Add(action);
        return ExecuteHandler(action, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
