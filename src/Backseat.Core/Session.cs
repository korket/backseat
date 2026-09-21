namespace Backseat.Core;

public sealed class Session : IAsyncDisposable
{
    private readonly IComputerBackend _backend;
    private readonly IRunRecorder? _recorder;
    private readonly ObservationSettlePolicy _settlePolicy;
    private readonly List<Observation> _observations = new();
    private readonly List<ActionRecord> _actions = new();
    private readonly CancellationTokenSource _lifetime = new();

    private TargetDescriptor? _target;
    private int _nextSequence = 1;

    public Session(
        IComputerBackend backend,
        IRunRecorder? recorder = null,
        Guid? id = null,
        ObservationSettlePolicy? settlePolicy = null)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _recorder = recorder;

        if (id == Guid.Empty)
        {
            throw new ArgumentException("Session id must not be empty.", nameof(id));
        }

        _settlePolicy = settlePolicy ?? ObservationSettlePolicy.Default;
        _settlePolicy.Validate();

        Id = id ?? Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public DateTimeOffset CreatedAt { get; }

    public IComputerBackend Backend => _backend;

    public IRunRecorder? Recorder => _recorder;

    public SessionState State { get; private set; } = SessionState.Created;

    public TargetDescriptor? Target => _target;

    public CancellationToken CancellationToken => _lifetime.Token;

    public IReadOnlyList<Observation> Observations => _observations;

    public IReadOnlyList<ActionRecord> Actions => _actions;

    public bool IsRecording { get; private set; }

    public string? RecordingPath { get; private set; }

    public async Task StartRecordingAsync(string outputDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Recording output directory must not be empty.", nameof(outputDirectory));
        }

        EnsureOpen();
        RequireTarget();

        if (IsRecording)
        {
            throw new InvalidOperationException("Recording is already active for this session.");
        }

        if (_backend is not IRecordingBackend recordingBackend)
        {
            throw new InvalidOperationException($"Backend '{_backend.Name}' does not support recording.");
        }

        await RunAsync(
            async token =>
            {
                await recordingBackend.StartRecordingAsync(outputDirectory, token);
            },
            cancellationToken);

        IsRecording = true;
        await ActivateAsync(cancellationToken);
        await NotifyAsync(recorder => recorder.OnRecordingChangedAsync(true, null, cancellationToken));
    }

    public async Task<string?> StopRecordingAsync(CancellationToken cancellationToken = default)
    {
        EnsureOpen();

        if (!IsRecording)
        {
            throw new InvalidOperationException("Recording is not active for this session.");
        }

        if (_backend is not IRecordingBackend recordingBackend)
        {
            throw new InvalidOperationException($"Backend '{_backend.Name}' does not support recording.");
        }

        var path = await RunAsync(token => recordingBackend.StopRecordingAsync(token), cancellationToken);

        IsRecording = false;
        RecordingPath = path ?? RecordingPath;
        await NotifyAsync(recorder => recorder.OnRecordingChangedAsync(false, path, cancellationToken));

        return path;
    }

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
        await NotifyStateAsync(SessionState.TargetSelected, cancellationToken);
        await NotifyAsync(recorder => recorder.OnTargetSelectedAsync(match, cancellationToken));
        return match;
    }

    public async Task<Observation> ObserveAsync(CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        var target = RequireTarget();

        var attempt = 1;
        var observation = await RunAsync(token => _backend.ObserveAsync(target, token), cancellationToken);

        while (observation.IsDegraded && attempt < _settlePolicy.MaxAttempts)
        {
            await Task.Delay(_settlePolicy.Delay, cancellationToken);
            attempt++;
            observation = await RunAsync(token => _backend.ObserveAsync(target, token), cancellationToken);
        }

        _observations.Add(observation);
        await ActivateAsync(cancellationToken);
        await NotifyAsync(recorder => recorder.OnObservationAsync(observation, cancellationToken));

        return observation;
    }

    public async Task<ActionReceipt> ExecuteAsync(ComputerAction action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        EnsureOpen();
        var target = RequireTarget();

        var sequence = _nextSequence++;

        var receipt = await RunAsync(token => _backend.ExecuteAsync(target, action, token), cancellationToken);

        var record = new ActionRecord(sequence, action, receipt, DateTimeOffset.UtcNow);
        _actions.Add(record);
        await ActivateAsync(cancellationToken);
        await NotifyAsync(recorder => recorder.OnActionAsync(record, cancellationToken));

        return receipt;
    }

    public async Task CloseAsync()
    {
        if (State is SessionState.Closing or SessionState.Closed)
        {
            return;
        }

        State = SessionState.Closing;
        await NotifyStateAsync(SessionState.Closing, CancellationToken.None);

        if (IsRecording)
        {
            string? path = null;
            try
            {
                if (_backend is IRecordingBackend recordingBackend)
                {
                    path = await recordingBackend.StopRecordingAsync(CancellationToken.None);
                }
            }
            catch (Exception)
            {
                path = null;
            }

            IsRecording = false;
            RecordingPath = path ?? RecordingPath;
            await NotifyAsync(recorder => recorder.OnRecordingChangedAsync(false, path, CancellationToken.None));
        }

        _lifetime.Cancel();

        if (_backend is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }

        State = SessionState.Closed;
        await NotifyStateAsync(SessionState.Closed, CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        _lifetime.Dispose();
    }

    private async Task ActivateAsync(CancellationToken cancellationToken)
    {
        if (State == SessionState.TargetSelected)
        {
            State = SessionState.Active;
            await NotifyStateAsync(SessionState.Active, cancellationToken);
        }
    }

    private async Task NotifyStateAsync(SessionState state, CancellationToken cancellationToken)
    {
        if (_recorder is not null)
        {
            await _recorder.OnStateChangedAsync(state, cancellationToken);
        }
    }

    private async Task NotifyAsync(Func<IRunRecorder, ValueTask> notification)
    {
        if (_recorder is not null)
        {
            await notification(_recorder);
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

    private async Task RunAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            await operation(_lifetime.Token);
            return;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, cancellationToken);
        await operation(linked.Token);
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
