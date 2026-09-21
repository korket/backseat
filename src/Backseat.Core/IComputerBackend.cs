namespace Backseat.Core;

public interface IComputerBackend
{
    string Name { get; }

    BackendCapabilities Capabilities { get; }

    Task<IReadOnlyList<TargetDescriptor>> DiscoverTargetsAsync(CancellationToken cancellationToken = default);

    Task<Observation> ObserveAsync(TargetDescriptor target, CancellationToken cancellationToken = default);

    Task<ActionReceipt> ExecuteAsync(TargetDescriptor target, ComputerAction action, CancellationToken cancellationToken = default);
}
