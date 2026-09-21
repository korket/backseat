using System.Text.Json;
using Backseat.Core;
using Backseat.Core.Runs;

namespace Backseat.Core.Tests;

public sealed class RunWriterTests : IDisposable
{
    private static readonly TargetDescriptor KnownTarget = new()
    {
        ProcessId = 42,
        WindowId = 99,
        Title = "Untitled - Notepad",
    };

    private readonly string _root = Path.Combine(Path.GetTempPath(), "backseat-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task Create_Writes_Metadata()
    {
        var runId = Guid.NewGuid();

        await using var writer = await RunWriter.CreateAsync(_root, runId, "fake");

        var metadata = ReadMetadata(writer.RunDirectory);
        Assert.Equal(runId.ToString("D"), metadata.GetProperty("runId").GetString());
        Assert.Equal("fake", metadata.GetProperty("backend").GetString());
        Assert.True(File.Exists(Path.Combine(writer.RunDirectory, "metadata.json")));
    }

    [Fact]
    public async Task Session_Id_Can_Match_The_Run_Id()
    {
        var runId = Guid.NewGuid();
        var session = new Session(new FakeBackend(), id: runId);

        Assert.Equal(runId, session.Id);
        await session.CloseAsync();
    }

    [Fact]
    public async Task Session_With_RunWriter_Produces_Logs_And_Final_Metadata()
    {
        var backend = new FakeBackend();
        backend.Targets.Add(KnownTarget);

        var runId = Guid.NewGuid();

        await using (var writer = await RunWriter.CreateAsync(_root, runId, backend.Name))
        {
            var session = new Session(backend, writer, runId);
            await session.SelectTargetAsync(KnownTarget);
            await session.ObserveAsync();
            await session.ExecuteAsync(new ClickAction(1, 2));
            await session.CloseAsync();

            var actions = ReadLines(Path.Combine(writer.RunDirectory, "actions.jsonl"));
            Assert.Single(actions);
            Assert.Equal(1, actions[0].GetProperty("sequence").GetInt32());
            Assert.Equal("click", actions[0].GetProperty("action").GetProperty("type").GetString());
            Assert.Equal("Confirmed", actions[0].GetProperty("receipt").GetProperty("effect").GetString());
            Assert.Equal("Background", actions[0].GetProperty("receipt").GetProperty("delivery").GetString());

            var events = ReadLines(Path.Combine(writer.RunDirectory, "events.jsonl"));
            var kinds = events.Select(e => e.GetProperty("kind").GetString()).ToList();
            Assert.Contains("state", kinds);
            Assert.Contains("observation", kinds);

            var states = events.Where(e => e.GetProperty("kind").GetString() == "state")
                .Select(e => e.GetProperty("state").GetString())
                .ToList();
            Assert.Equal(new[] { "TargetSelected", "Active", "Closing", "Closed" }, states);

            var metadata = ReadMetadata(writer.RunDirectory);
            Assert.Equal("Closed", metadata.GetProperty("finalState").GetString());
            Assert.Equal(1, metadata.GetProperty("observationCount").GetInt32());
            Assert.Equal(1, metadata.GetProperty("actionCount").GetInt32());
            Assert.Equal(42u, metadata.GetProperty("target").GetProperty("processId").GetUInt32());
        }
    }

    [Fact]
    public async Task Screenshots_Are_Opt_In()
    {
        var runId = Guid.NewGuid();
        var observation = new Observation
        {
            Target = KnownTarget,
            Timestamp = DateTimeOffset.UtcNow,
            ScreenshotPng = new byte[] { 1, 2, 3 },
        };

        await using (var writer = await RunWriter.CreateAsync(_root, runId, "fake"))
        {
            await writer.OnObservationAsync(observation);
            Assert.False(Directory.Exists(Path.Combine(writer.RunDirectory, "screenshots")));
        }

        await using (var writer = await RunWriter.CreateAsync(_root, runId, "fake", saveObservationScreenshots: true))
        {
            await writer.OnObservationAsync(observation);
            var screenshot = Path.Combine(writer.RunDirectory, "screenshots", "obs-00001.png");
            Assert.True(File.Exists(screenshot));
            Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(screenshot));
        }
    }

    [Fact]
    public async Task Failed_Receipts_Persist_Unchanged()
    {
        await using var writer = await RunWriter.CreateAsync(_root, Guid.NewGuid(), "fake");

        var record = new ActionRecord(
            7,
            new ClickAction(1, 2),
            new ActionReceipt { Effect = ActionEffect.Failed, Error = "daemon gone" },
            DateTimeOffset.UtcNow);

        await writer.OnActionAsync(record);

        var actions = ReadLines(Path.Combine(writer.RunDirectory, "actions.jsonl"));
        Assert.Single(actions);
        Assert.Equal("Failed", actions[0].GetProperty("receipt").GetProperty("effect").GetString());
        Assert.Equal("daemon gone", actions[0].GetProperty("receipt").GetProperty("error").GetString());
    }

    [Fact]
    public async Task Recording_Events_Capture_The_Artifact_Path()
    {
        await using var writer = await RunWriter.CreateAsync(_root, Guid.NewGuid(), "fake");

        await writer.OnRecordingChangedAsync(true, null);
        await writer.OnRecordingChangedAsync(false, @"C:\runs\x\recording.mp4");

        Assert.Equal(@"C:\runs\x\recording.mp4", writer.RecordingPath);

        var events = ReadLines(Path.Combine(writer.RunDirectory, "events.jsonl"));
        var recording = events.Where(e => e.GetProperty("kind").GetString() == "recording").ToList();
        Assert.Equal(2, recording.Count);
        Assert.True(recording[0].GetProperty("isRecording").GetBoolean());
        Assert.Equal(@"C:\runs\x\recording.mp4", recording[1].GetProperty("artifactPath").GetString());
    }

    private static JsonElement ReadMetadata(string runDirectory)
    {
        var json = ReadAllTextShared(Path.Combine(runDirectory, "metadata.json"));
        return JsonDocument.Parse(json).RootElement;
    }

    private static List<JsonElement> ReadLines(string path) =>
        ReadAllLinesShared(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonDocument.Parse(line).RootElement)
            .ToList();

    private static string ReadAllTextShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string[] ReadAllLinesShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        return lines.ToArray();
    }
}
