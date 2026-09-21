using Backseat.Core;

namespace Backseat.Core.Tests;

internal class FakeBackend : IComputerBackend, IAsyncDisposable
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

    public int ObserveCalls { get; private set; }

    public Func<TargetDescriptor, CancellationToken, Task<Observation>> ObserveHandler { get; set; } =
        (target, _) => Task.FromResult(new Observation
        {
            Target = target,
            Timestamp = DateTimeOffset.UtcNow,
            AccessibilityTree = "- Window",
        });

    public Task<Observation> ObserveAsync(TargetDescriptor target, CancellationToken cancellationToken = default)
    {
        ObserveCalls++;
        return ObserveHandler(target, cancellationToken);
    }

    public Task<ActionReceipt> ExecuteAsync(TargetDescriptor target, ComputerAction action, CancellationToken cancellationToken = default)
    {
        ExecutedActions.Add(action);
        return ExecuteHandler(action, cancellationToken);
    }

    public virtual ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}

internal sealed class RecordingFakeBackend : FakeBackend, IRecordingBackend
{
    public List<string> StartedRecordings { get; } = new();

    public int StopCalls { get; private set; }

    public Func<Task<string?>> StopHandler { get; set; } = () => Task.FromResult<string?>("recording.mp4");

    public Task StartRecordingAsync(string outputDirectory, CancellationToken cancellationToken = default)
    {
        StartedRecordings.Add(outputDirectory);
        return Task.CompletedTask;
    }

    public Task<string?> StopRecordingAsync(CancellationToken cancellationToken = default)
    {
        StopCalls++;
        return StopHandler();
    }
}

internal sealed class FakeRecorder : IRunRecorder
{
    public List<string> Events { get; } = new();

    public ValueTask OnStateChangedAsync(SessionState state, CancellationToken cancellationToken = default)
    {
        Events.Add($"state:{state}");
        return ValueTask.CompletedTask;
    }

    public ValueTask OnTargetSelectedAsync(TargetDescriptor target, CancellationToken cancellationToken = default)
    {
        Events.Add($"target:{target.ProcessId}");
        return ValueTask.CompletedTask;
    }

    public ValueTask OnObservationAsync(Observation observation, CancellationToken cancellationToken = default)
    {
        Events.Add("observation");
        return ValueTask.CompletedTask;
    }

    public ValueTask OnActionAsync(ActionRecord record, CancellationToken cancellationToken = default)
    {
        Events.Add($"action:{record.Sequence}");
        return ValueTask.CompletedTask;
    }

    public ValueTask OnRecordingChangedAsync(bool isRecording, string? artifactPath, CancellationToken cancellationToken = default)
    {
        Events.Add($"recording:{isRecording}:{artifactPath ?? "none"}");
        return ValueTask.CompletedTask;
    }
}
