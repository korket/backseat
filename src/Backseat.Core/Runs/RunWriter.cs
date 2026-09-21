using System.Security.Cryptography;
using System.Text.Json;

namespace Backseat.Core.Runs;

public sealed class RunWriter : IRunRecorder, IAsyncDisposable
{
    private static readonly JsonSerializerOptions CompactOptions = new() { WriteIndented = false };
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    private readonly string _runDirectory;
    private readonly bool _saveObservationScreenshots;
    private readonly int _maxObservationScreenshots;
    private readonly Queue<string> _screenshotPaths = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _runId;
    private readonly string _backendName;
    private readonly DateTimeOffset _createdAt;

    private StreamWriter? _actionsWriter;
    private StreamWriter? _eventsWriter;
    private int _observationCount;
    private int _actionCount;
    private string? _lastScreenshotHash;
    private TargetDescriptor? _target;
    private bool _disposed;

    private RunWriter(
        string runDirectory,
        string runId,
        string backendName,
        DateTimeOffset createdAt,
        bool saveObservationScreenshots,
        int maxObservationScreenshots)
    {
        _runDirectory = runDirectory;
        _runId = runId;
        _backendName = backendName;
        _createdAt = createdAt;
        _saveObservationScreenshots = saveObservationScreenshots;
        _maxObservationScreenshots = maxObservationScreenshots;
    }

    public string RunDirectory => _runDirectory;

    public string? RecordingPath { get; private set; }

    public static async Task<RunWriter> CreateAsync(
        string rootDirectory,
        Guid runId,
        string backendName,
        bool saveObservationScreenshots = false,
        int maxObservationScreenshots = 50,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("Root directory must not be empty.", nameof(rootDirectory));
        }

        if (string.IsNullOrWhiteSpace(backendName))
        {
            throw new ArgumentException("Backend name must not be empty.", nameof(backendName));
        }

        if (maxObservationScreenshots < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxObservationScreenshots), maxObservationScreenshots, "At least one screenshot must be retained.");
        }

        var directory = Path.Combine(rootDirectory, runId.ToString("D"));
        Directory.CreateDirectory(directory);

        var writer = new RunWriter(
            directory,
            runId.ToString("D"),
            backendName,
            DateTimeOffset.UtcNow,
            saveObservationScreenshots,
            maxObservationScreenshots);

        await writer.WriteMetadataAsync(finalState: null, cancellationToken);
        return writer;
    }

    public async ValueTask OnStateChangedAsync(SessionState state, CancellationToken cancellationToken = default)
    {
        await AppendEventAsync(new { timestamp = DateTimeOffset.UtcNow, kind = "state", state = state.ToString() }, cancellationToken);

        if (state == SessionState.Closed)
        {
            await WriteMetadataAsync(state, cancellationToken);
        }
    }

    public async ValueTask OnTargetSelectedAsync(TargetDescriptor target, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);

        _target = target;

        await AppendEventAsync(new
        {
            timestamp = DateTimeOffset.UtcNow,
            kind = "target",
            target = DescribeTarget(target),
        }, cancellationToken);
    }

    public async ValueTask OnObservationAsync(Observation observation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observation);

        _observationCount++;
        var index = _observationCount;

        var screenshotWritten = false;
        string? screenshotSkipped = null;

        if (_saveObservationScreenshots && observation.ScreenshotPng is not null)
        {
            var hash = Convert.ToHexString(SHA256.HashData(observation.ScreenshotPng));

            if (hash == _lastScreenshotHash)
            {
                screenshotSkipped = "duplicate";
            }
            else
            {
                _lastScreenshotHash = hash;
                await WriteScreenshotAsync(index, observation.ScreenshotPng, cancellationToken);
                screenshotWritten = true;
            }
        }

        await AppendEventAsync(new
        {
            timestamp = observation.Timestamp,
            kind = "observation",
            index,
            target = DescribeTarget(observation.Target),
            hasScreenshot = observation.ScreenshotPng is not null,
            screenshotWritten,
            screenshotSkipped,
            treeLength = observation.AccessibilityTree?.Length ?? 0,
        }, cancellationToken);
    }

    public async ValueTask OnActionAsync(ActionRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        _actionCount++;
        var payload = new
        {
            sequence = record.Sequence,
            timestamp = record.Timestamp,
            action = DescribeAction(record.Action),
            receipt = DescribeReceipt(record.Receipt),
        };

        await AppendLineAsync("actions.jsonl", payload, CompactOptions, cancellationToken);
    }

    public async ValueTask OnRecordingChangedAsync(bool isRecording, string? artifactPath, CancellationToken cancellationToken = default)
    {
        if (artifactPath is not null)
        {
            RecordingPath = artifactPath;
        }

        await AppendEventAsync(new
        {
            timestamp = DateTimeOffset.UtcNow,
            kind = "recording",
            isRecording,
            artifactPath,
        }, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _gate.WaitAsync();

        try
        {
            if (_actionsWriter is not null)
            {
                await _actionsWriter.DisposeAsync();
                _actionsWriter = null;
            }

            if (_eventsWriter is not null)
            {
                await _eventsWriter.DisposeAsync();
                _eventsWriter = null;
            }
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private async Task WriteScreenshotAsync(int index, byte[] png, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(_runDirectory, "screenshots");
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"obs-{index:D5}.png");
        await File.WriteAllBytesAsync(path, png, cancellationToken);

        _screenshotPaths.Enqueue(path);
        while (_screenshotPaths.Count > _maxObservationScreenshots)
        {
            var oldest = _screenshotPaths.Dequeue();
            if (File.Exists(oldest))
            {
                File.Delete(oldest);
            }
        }
    }

    private async Task WriteMetadataAsync(SessionState? finalState, CancellationToken cancellationToken)
    {
        var metadata = new
        {
            runId = _runId,
            createdAt = _createdAt,
            backend = _backendName,
            target = _target is null ? null : DescribeTarget(_target),
            closedAt = finalState is null ? (DateTimeOffset?)null : DateTimeOffset.UtcNow,
            finalState = finalState?.ToString(),
            observationCount = _observationCount,
            actionCount = _actionCount,
            recordingPath = RecordingPath,
        };

        var path = Path.Combine(_runDirectory, "metadata.json");
        var json = JsonSerializer.Serialize(metadata, IndentedOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    private async ValueTask AppendEventAsync(object payload, CancellationToken cancellationToken)
    {
        await AppendLineAsync("events.jsonl", payload, CompactOptions, cancellationToken);
    }

    private async ValueTask AppendLineAsync(string fileName, object payload, JsonSerializerOptions options, CancellationToken cancellationToken)
    {
        var line = JsonSerializer.Serialize(payload, options);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var writer = fileName == "actions.jsonl"
                ? _actionsWriter ??= CreateWriter(fileName)
                : _eventsWriter ??= CreateWriter(fileName);

            await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
            await writer.FlushAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private StreamWriter CreateWriter(string fileName) =>
        new(new FileStream(Path.Combine(_runDirectory, fileName), FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            AutoFlush = false,
        };

    private static object DescribeTarget(TargetDescriptor target) => new
    {
        processId = target.ProcessId,
        windowId = target.WindowId,
        executablePath = target.ExecutablePath,
        title = target.Title,
    };

    private static object DescribeAction(ComputerAction action) => action switch
    {
        ClickAction click => new { type = "click", click.X, click.Y, click.ElementToken, click.RequireBackgroundSafe },
        TypeTextAction type => new { type = "type_text", type.Text },
        PressKeyAction key => new { type = "press_key", key.Key },
        ScrollAction scroll => new { type = "scroll", axis = scroll.Axis.ToString(), scroll.Ticks },
        WaitAction wait => new { type = "wait", durationMs = wait.Duration.TotalMilliseconds },
        _ => new { type = action.GetType().Name },
    };

    private static object DescribeReceipt(ActionReceipt receipt) => new
    {
        effect = receipt.Effect.ToString(),
        delivery = receipt.Delivery.ToString(),
        route = receipt.DeliveryRoute,
        foregroundChanged = receipt.ForegroundChanged,
        cursorMoved = receipt.CursorMoved,
        suggestedEscalation = receipt.SuggestedEscalation?.ToString(),
        warnings = receipt.Warnings,
        error = receipt.Error,
        durationMs = receipt.Duration?.TotalMilliseconds,
    };
}
