namespace Backseat.Core;

public sealed class Session : IAsyncDisposable
{
    private readonly IComputerBackend _backend;
    private readonly List<Observation> _observations = new();
    private readonly List<ActionRecord> _actions = new();
    private readonly CancellationTokenSource _lifetime = new();

    private TargetDescriptor? _target;
    private int _nextSequence = 1;

    public Session(IComputerBackend backend)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public DateTimeOffset CreatedAt { get; }

    public IComputerBackend Backend => _backend;

    public SessionState State { get; private set; } = SessionState.Created;

    public TargetDescriptor? Target => _target;

    public CancellationToken CancellationToken => _lifetime.Token;

    public IReadOnlyList<Observation> Observations => _observations;

    public IReadOnlyList<ActionRecord> Actions => _actions;

    public async Task<TargetDescriptor> SelectTargetAsync(TargetDescriptor target, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        EnsureOpen();

        if (State != SessionState.Created)
        {
            throw new InvalidOperationException("A target is already selected; early milestones allow one primary target per session.");
        }

        var discovered = await RunAsync(token => _backend.DiscoverTargetsAsync(token), cancellationToken);

        var match = discovered.FirstOrDefault(candidate =>
            candidate.ProcessId == target.ProcessId && candidate.WindowId == target.WindowId);

        if (match is null)
        {
            throw new InvalidOperationException(
                $"Target pid {target.ProcessId} window {target.WindowId?.ToString() ?? "(none)"} was not reported by backend '{_backend.Name}'.");
        }

        _target = match;
        State = SessionState.TargetSelected;
        return match;
    }

    public async Task<Observation> ObserveAsync(CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        var target = RequireTarget();

        var observation = await RunAsync(token => _backend.ObserveAsync(target, token), cancellationToken);
        _observations.Add(observation);
        Activate();

        return observation;
    }

    public async Task<ActionReceipt> ExecuteAsync(ComputerAction action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        EnsureOpen();
        var target = RequireTarget();

        var sequence = _nextSequence++;

        var receipt = await RunAsync(token => _backend.ExecuteAsync(target, action, token), cancellationToken);

        _actions.Add(new ActionRecord(sequence, action, receipt, DateTimeOffset.UtcNow));
        Activate();

        return receipt;
    }

    public async Task CloseAsync()
    {
        if (State is SessionState.Closing or SessionState.Closed)
        {
            return;
        }

        State = SessionState.Closing;
        _lifetime.Cancel();

        if (_backend is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }

        State = SessionState.Closed;
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        _lifetime.Dispose();
    }

    private void Activate()
    {
        if (State == SessionState.TargetSelected)
        {
            State = SessionState.Active;
        }
    }

    private TargetDescriptor RequireTarget() =>
        _target ?? throw new InvalidOperationException("Select a target before using the session.");

    private void EnsureOpen()
    {
        if (State is SessionState.Closing or SessionState.Closed)
        {
            throw new ObjectDisposedException(nameof(Session));
        }
    }

    private async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            return await operation(_lifetime.Token);
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, cancellationToken);
        return await operation(linked.Token);
    }
}
