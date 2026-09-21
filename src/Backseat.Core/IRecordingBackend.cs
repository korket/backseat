namespace Backseat.Core;

public interface IRecordingBackend
{
    Task StartRecordingAsync(string outputDirectory, CancellationToken cancellationToken = default);

    Task<string?> StopRecordingAsync(CancellationToken cancellationToken = default);
}
