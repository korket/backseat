namespace Backseat.Core;

public interface IRunRecorder
{
    ValueTask OnStateChangedAsync(SessionState state, CancellationToken cancellationToken = default);

    ValueTask OnTargetSelectedAsync(TargetDescriptor target, CancellationToken cancellationToken = default);

    ValueTask OnObservationAsync(Observation observation, CancellationToken cancellationToken = default);

    ValueTask OnActionAsync(ActionRecord record, CancellationToken cancellationToken = default);

    ValueTask OnRecordingChangedAsync(bool isRecording, string? artifactPath, CancellationToken cancellationToken = default);
}
