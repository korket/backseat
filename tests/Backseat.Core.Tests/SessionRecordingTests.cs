using Backseat.Core;

namespace Backseat.Core.Tests;

public sealed class SessionRecordingTests
{
    private static readonly TargetDescriptor KnownTarget = new() { ProcessId = 42, WindowId = 99 };

    [Fact]
    public async Task StartRecording_Delegates_To_The_Backend_And_Notifies_The_Recorder()
    {
        var backend = new RecordingFakeBackend();
        backend.Targets.Add(KnownTarget);
        var recorder = new FakeRecorder();
        var session = new Session(backend, recorder);
        await session.SelectTargetAsync(KnownTarget);

        await session.StartRecordingAsync(@"C:\runs\demo");

        Assert.True(session.IsRecording);
        Assert.Equal(@"C:\runs\demo", Assert.Single(backend.StartedRecordings));
        Assert.Equal(SessionState.Active, session.State);
        Assert.Contains("recording:True:none", recorder.Events);
    }

    [Fact]
    public async Task StopRecording_Returns_The_Artifact_And_Records_The_Path()
    {
        var backend = new RecordingFakeBackend();
        backend.Targets.Add(KnownTarget);
        var session = new Session(backend);
        await session.SelectTargetAsync(KnownTarget);
        await session.StartRecordingAsync(@"C:\runs\demo");

        var path = await session.StopRecordingAsync();

        Assert.Equal("recording.mp4", path);
        Assert.False(session.IsRecording);
        Assert.Equal("recording.mp4", session.RecordingPath);
        Assert.Equal(1, backend.StopCalls);
    }

    [Fact]
    public async Task Recording_Requires_Backend_Support()
    {
        var backend = new FakeBackend();
        backend.Targets.Add(KnownTarget);
        var session = new Session(backend);
        await session.SelectTargetAsync(KnownTarget);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => session.StartRecordingAsync(@"C:\runs\demo"));

        Assert.Contains("does not support recording", exception.Message);
    }

    [Fact]
    public async Task Recording_Requires_A_Selected_Target()
    {
        var session = new Session(new RecordingFakeBackend());

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.StartRecordingAsync(@"C:\runs\demo"));
    }

    [Fact]
    public async Task StartRecording_Rejects_An_Empty_Directory()
    {
        var backend = new RecordingFakeBackend();
        backend.Targets.Add(KnownTarget);
        var session = new Session(backend);
        await session.SelectTargetAsync(KnownTarget);

        await Assert.ThrowsAsync<ArgumentException>(() => session.StartRecordingAsync(" "));
    }

    [Fact]
    public async Task StopRecording_Requires_An_Active_Recording()
    {
        var backend = new RecordingFakeBackend();
        backend.Targets.Add(KnownTarget);
        var session = new Session(backend);
        await session.SelectTargetAsync(KnownTarget);

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.StopRecordingAsync());
    }

    [Fact]
    public async Task Close_While_Recording_Stops_It_And_Still_Closes()
    {
        var backend = new RecordingFakeBackend();
        backend.Targets.Add(KnownTarget);
        var recorder = new FakeRecorder();
        var session = new Session(backend, recorder);
        await session.SelectTargetAsync(KnownTarget);
        await session.StartRecordingAsync(@"C:\runs\demo");

        await session.CloseAsync();

        Assert.Equal(SessionState.Closed, session.State);
        Assert.False(session.IsRecording);
        Assert.Equal(1, backend.StopCalls);
        Assert.Equal("recording.mp4", session.RecordingPath);
        Assert.Contains("recording:False:recording.mp4", recorder.Events);
    }

    [Fact]
    public async Task Close_While_Recording_Survives_Stop_Failure()
    {
        var backend = new RecordingFakeBackend
        {
            StopHandler = () => throw new InvalidOperationException("driver gone"),
        };
        backend.Targets.Add(KnownTarget);
        var session = new Session(backend);
        await session.SelectTargetAsync(KnownTarget);
        await session.StartRecordingAsync(@"C:\runs\demo");

        await session.CloseAsync();

        Assert.Equal(SessionState.Closed, session.State);
        Assert.False(session.IsRecording);
        Assert.Null(session.RecordingPath);
    }
}
