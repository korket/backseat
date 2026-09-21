using Backseat.Core;

namespace Backseat.Backends.Cua.Tests;

public sealed class CuaCliBackendTests
{
    private static readonly TargetDescriptor Target = new() { ProcessId = 42, WindowId = 99 };

    [Fact]
    public async Task BackgroundConfirmed_Click_Confirms_Background_Safe()
    {
        var cli = new FakeCuaCli().Enqueue(0, """
            {"delivery":{"mode":"background"},"effect":"confirmed","route":"accessibility"}
            """);
        var backend = new CuaCliBackend(cli);

        var receipt = await backend.ExecuteAsync(Target, new ClickAction(10, 20));

        Assert.Equal("click", cli.Calls[0].Tool);
        Assert.True(receipt.ConfirmsBackgroundSafe);
        Assert.Equal(ActionEffect.Confirmed, receipt.Effect);
        Assert.Equal(ActionDelivery.Background, receipt.Delivery);
        Assert.Equal("accessibility", receipt.DeliveryRoute);

        var arguments = cli.ParseArguments(0);
        Assert.Equal(42u, arguments.GetProperty("pid").GetUInt32());
        Assert.Equal(10, arguments.GetProperty("x").GetDouble());
        Assert.Equal(20, arguments.GetProperty("y").GetDouble());
        Assert.Equal("background", arguments.GetProperty("delivery_mode").GetString());
        Assert.False(arguments.TryGetProperty("element_token", out _));
    }

    [Fact]
    public async Task Click_With_ElementToken_Prefers_Token_Over_Coordinates()
    {
        var cli = new FakeCuaCli().Enqueue(0, """
            {"delivery":{"mode":"background"},"effect":"confirmed","route":"accessibility"}
            """);
        var backend = new CuaCliBackend(cli);

        await backend.ExecuteAsync(Target, new ClickAction("s00000003:31"));

        var arguments = cli.ParseArguments(0);
        Assert.Equal("s00000003:31", arguments.GetProperty("element_token").GetString());
        Assert.False(arguments.TryGetProperty("x", out _));
    }

    [Fact]
    public async Task Unverifiable_Result_Does_Not_Claim_Background_Safe_And_Keeps_Escalation()
    {
        var cli = new FakeCuaCli().Enqueue(0, """
            {"delivery":{"mode":"background"},"effect":"unverifiable",
             "escalation":{"reason":"delivery_failed","target":"foreground"},
             "route":"synthetic_events"}
            """);
        var backend = new CuaCliBackend(cli);

        var receipt = await backend.ExecuteAsync(Target, new PressKeyAction("escape"));

        Assert.False(receipt.ConfirmsBackgroundSafe);
        Assert.Equal(ActionEffect.Unverifiable, receipt.Effect);
        Assert.Equal(ActionDelivery.Foreground, receipt.SuggestedEscalation);
        Assert.Contains("escalation: delivery_failed", receipt.Warnings);
        Assert.Null(receipt.Error);
    }

    [Fact]
    public async Task Foreground_Result_Never_Claims_Background_Safe()
    {
        var cli = new FakeCuaCli().Enqueue(0, """
            {"delivery":{"mode":"foreground"},"effect":"confirmed","route":"global_input"}
            """);
        var backend = new CuaCliBackend(cli);

        var receipt = await backend.ExecuteAsync(Target, new ClickAction(1, 2));

        Assert.Equal(ActionDelivery.Foreground, receipt.Delivery);
        Assert.False(receipt.ConfirmsBackgroundSafe);
    }

    [Fact]
    public async Task NonZero_Exit_Maps_To_Failed_Receipt_With_Error_Text()
    {
        var cli = new FakeCuaCli().Enqueue(1, string.Empty, "incompatible daemon");
        var backend = new CuaCliBackend(cli);

        var receipt = await backend.ExecuteAsync(Target, new ClickAction(1, 2));

        Assert.Equal(ActionEffect.Failed, receipt.Effect);
        Assert.Contains("incompatible daemon", receipt.Error);
        Assert.False(receipt.ConfirmsBackgroundSafe);
    }

    [Fact]
    public async Task Refusal_Maps_To_Failed_Receipt()
    {
        var cli = new FakeCuaCli().Enqueue(0, """
            {"refusal":{"code":"foreign_process_termination_denied","message":"not ours"},"status":"refused"}
            """);
        var backend = new CuaCliBackend(cli);

        var receipt = await backend.ExecuteAsync(Target, new TypeTextAction("hello"));

        Assert.Equal(ActionEffect.Failed, receipt.Effect);
        Assert.Contains("foreign_process_termination_denied", receipt.Error);
        Assert.Contains("not ours", receipt.Error);
    }

    [Fact]
    public async Task Malformed_Output_Maps_To_Failed_Receipt()
    {
        var cli = new FakeCuaCli().Enqueue(0, "not json at all");
        var backend = new CuaCliBackend(cli);

        var receipt = await backend.ExecuteAsync(Target, new ClickAction(1, 2));

        Assert.Equal(ActionEffect.Failed, receipt.Effect);
        Assert.Contains("unparseable", receipt.Error);
    }

    [Fact]
    public async Task Wait_Is_Handled_Locally_Without_Invoking_The_Cli()
    {
        var cli = new FakeCuaCli();
        var backend = new CuaCliBackend(cli);

        var receipt = await backend.ExecuteAsync(Target, new WaitAction(TimeSpan.FromMilliseconds(1)));

        Assert.Empty(cli.Calls);
        Assert.Equal(ActionEffect.Confirmed, receipt.Effect);
        Assert.Equal("local", receipt.DeliveryRoute);
        Assert.False(receipt.ConfirmsBackgroundSafe);
    }

    [Fact]
    public async Task TypeText_Passes_Text_And_Background_Mode()
    {
        var cli = new FakeCuaCli().Enqueue(0, """
            {"delivery":{"mode":"background"},"effect":"confirmed","route":"accessibility"}
            """);
        var backend = new CuaCliBackend(cli);

        await backend.ExecuteAsync(Target, new TypeTextAction("Backseat"));

        Assert.Equal("type_text", cli.Calls[0].Tool);
        var arguments = cli.ParseArguments(0);
        Assert.Equal("Backseat", arguments.GetProperty("text").GetString());
        Assert.Equal("background", arguments.GetProperty("delivery_mode").GetString());
    }

    [Fact]
    public async Task Scroll_Maps_Axis_And_Direction_To_Ticks()
    {
        var cli = new FakeCuaCli()
            .Enqueue(0, """{"delivery":{"mode":"background"},"effect":"unverifiable","route":"synthetic_events"}""")
            .Enqueue(0, """{"delivery":{"mode":"background"},"effect":"unverifiable","route":"synthetic_events"}""");
        var backend = new CuaCliBackend(cli);

        await backend.ExecuteAsync(Target, new ScrollAction(ScrollAxis.Vertical, -2));
        await backend.ExecuteAsync(Target, new ScrollAction(ScrollAxis.Horizontal, 100));

        var up = cli.ParseArguments(0);
        Assert.Equal("up", up.GetProperty("direction").GetString());
        Assert.Equal(2, up.GetProperty("amount").GetInt32());
        Assert.Equal("line", up.GetProperty("by").GetString());

        var right = cli.ParseArguments(1);
        Assert.Equal("right", right.GetProperty("direction").GetString());
        Assert.Equal(50, right.GetProperty("amount").GetInt32());
    }

    [Fact]
    public async Task Discovery_Parses_Windows()
    {
        var cli = new FakeCuaCli().Enqueue(0, """
            {"windows":[
              {"app_name":"Notepad.exe","pid":7,"title":"Untitled - Notepad","window_id":100,"z_index":1},
              {"app_name":"explorer.exe","pid":8,"title":"Program Manager","window_id":200,"z_index":0}
            ]}
            """);
        var backend = new CuaCliBackend(cli);

        var targets = await backend.DiscoverTargetsAsync();

        Assert.Equal(2, targets.Count);
        Assert.Equal(7u, targets[0].ProcessId);
        Assert.Equal(100ul, targets[0].WindowId);
        Assert.Equal("Untitled - Notepad", targets[0].Title);
        Assert.Null(cli.Calls[0].Arguments);
    }

    [Fact]
    public async Task Discovery_Failure_Throws()
    {
        var cli = new FakeCuaCli().Enqueue(1, string.Empty, "daemon gone");
        var backend = new CuaCliBackend(cli);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => backend.DiscoverTargetsAsync());

        Assert.Contains("daemon gone", exception.Message);
    }

    [Fact]
    public async Task Observation_Decodes_Screenshot_And_Tree()
    {
        var cli = new FakeCuaCli().Enqueue(0, """
            {"screenshot_png_b64":"AQID","tree_markdown":"- Window \"x\"","snapshot_id":"s1"}
            """);
        var backend = new CuaCliBackend(cli);

        var observation = await backend.ObserveAsync(Target);

        Assert.Equal(Target, observation.Target);
        Assert.Equal(new byte[] { 1, 2, 3 }, observation.ScreenshotPng);
        Assert.Equal("- Window \"x\"", observation.AccessibilityTree);

        var arguments = cli.ParseArguments(0);
        Assert.Equal(42u, arguments.GetProperty("pid").GetUInt32());
        Assert.Equal(99ul, arguments.GetProperty("window_id").GetUInt64());
    }

    [Fact]
    public async Task Observation_Requires_Window_Id()
    {
        var cli = new FakeCuaCli();
        var backend = new CuaCliBackend(cli);

        await Assert.ThrowsAsync<ArgumentException>(() => backend.ObserveAsync(new TargetDescriptor { ProcessId = 42 }));

        Assert.Empty(cli.Calls);
    }

    [Fact]
    public async Task StartRecording_Enables_Video_In_The_Given_Directory()
    {
        var cli = new FakeCuaCli().Enqueue(0, """
            {"enabled":true,"output_dir":"C:\\runs\\x","video_active":true}
            """);
        var backend = new CuaCliBackend(cli);

        await backend.StartRecordingAsync(@"C:\runs\x");

        Assert.Equal("start_recording", cli.Calls[0].Tool);
        var arguments = cli.ParseArguments(0);
        Assert.Equal(@"C:\runs\x", arguments.GetProperty("output_dir").GetString());
        Assert.True(arguments.GetProperty("record_video").GetBoolean());
    }

    [Fact]
    public async Task StartRecording_Fails_Loudly_When_Not_Enabled()
    {
        var cli = new FakeCuaCli().Enqueue(0, """{"enabled":false}""");
        var backend = new CuaCliBackend(cli);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => backend.StartRecordingAsync(@"C:\runs\x"));

        Assert.Contains("did not enable recording", exception.Message);
    }

    [Fact]
    public async Task StopRecording_Returns_The_Finalized_Path()
    {
        var cli = new FakeCuaCli().Enqueue(0, """
            {"enabled":false,"last_video_path":"C:\\runs\\x\\recording.mp4"}
            """);
        var backend = new CuaCliBackend(cli);

        var path = await backend.StopRecordingAsync();

        Assert.Equal("stop_recording", cli.Calls[0].Tool);
        Assert.Equal(@"C:\runs\x\recording.mp4", path);
    }

    [Fact]
    public async Task StopRecording_Returns_Null_When_No_Video_Exists()
    {
        var cli = new FakeCuaCli().Enqueue(0, """{"enabled":false,"last_video_path":null}""");
        var backend = new CuaCliBackend(cli);

        Assert.Null(await backend.StopRecordingAsync());
    }

    [Fact]
    public async Task Observation_Parses_Structured_Elements()
    {
        var cli = new FakeCuaCli().Enqueue(0, """
            {"screenshot_png_b64":"AQID","tree_markdown":"- Window",
             "elements":[
               {"role":"Window","label":"Calculator","element_token":"s1:0","depth":1,"enabled":true,
                "frame":{"x":10,"y":20,"w":300,"h":400},"actions":["set_value"]},
               {"role":"Button","label":"Six","element_token":"s1:31","depth":5,"enabled":false,"actions":["invoke"]},
               {"role":"Text","label":null,"value":"Display is 0","element_token":null,"depth":3}
             ]}
            """);
        var backend = new CuaCliBackend(cli);

        var observation = await backend.ObserveAsync(Target);

        Assert.Equal(3, observation.Elements.Count);

        var window = observation.Elements[0];
        Assert.Equal("Window", window.Role);
        Assert.Equal("Calculator", window.Label);
        Assert.Equal("s1:0", window.ElementToken);
        Assert.True(window.Enabled);
        Assert.Equal(new ObservationElementFrame(10, 20, 300, 400), window.Frame);
        Assert.Equal(new[] { "set_value" }, window.Actions);

        var button = observation.Elements[1];
        Assert.False(button.Enabled);
        Assert.Null(button.Frame);
        Assert.Equal(new[] { "invoke" }, button.Actions);
        Assert.Equal(5, button.Depth);

        Assert.Equal("Display is 0", observation.Elements[2].Value);
        Assert.Null(observation.Elements[2].ElementToken);
        Assert.Empty(observation.Elements[2].Actions);
    }

    [Fact]
    public async Task Observation_Without_Elements_Returns_Empty_List()
    {
        var cli = new FakeCuaCli().Enqueue(0, """{"screenshot_png_b64":"AQID"}""");
        var backend = new CuaCliBackend(cli);

        var observation = await backend.ObserveAsync(Target);

        Assert.Empty(observation.Elements);
    }

    [Fact]
    public async Task Observation_Parses_Degraded_State()
    {
        var cli = new FakeCuaCli().Enqueue(0, """
            {"degraded":true,"degraded_reason":"ax_tree_empty","elements":[],"screenshot_error":"no content"}
            """);
        var backend = new CuaCliBackend(cli);

        var observation = await backend.ObserveAsync(Target);

        Assert.True(observation.IsDegraded);
        Assert.Equal("ax_tree_empty", observation.DegradedReason);
    }

    [Fact]
    public async Task Observation_Is_Not_Degraded_When_The_Flag_Is_Absent()
    {
        var cli = new FakeCuaCli().Enqueue(0, """{"screenshot_png_b64":"AQID","elements":[]}""");
        var backend = new CuaCliBackend(cli);

        var observation = await backend.ObserveAsync(Target);

        Assert.False(observation.IsDegraded);
        Assert.Null(observation.DegradedReason);
    }

    [Fact]
    public async Task Foreground_Retry_Is_Off_By_Default()
    {
        var cli = new FakeCuaCli().Enqueue(0, """{"error":"background_unavailable"}""");
        var backend = new CuaCliBackend(cli);

        var receipt = await backend.ExecuteAsync(Target, new ClickAction(1, 2));

        Assert.Equal(ActionEffect.Failed, receipt.Effect);
        Assert.Single(cli.Calls);
    }

    [Fact]
    public async Task Foreground_Retry_Escalates_Once_When_Enabled()
    {
        var cli = new FakeCuaCli()
            .Enqueue(0, """{"error":"background_unavailable: no UIA peer"}""")
            .Enqueue(0, """{"delivery":{"mode":"foreground"},"effect":"confirmed","route":"global_input"}""");
        var backend = new CuaCliBackend(cli, allowForegroundRetry: true);

        var receipt = await backend.ExecuteAsync(Target, new ClickAction(1, 2));

        Assert.Equal(2, cli.Calls.Count);
        Assert.Equal("background", cli.ParseArguments(0).GetProperty("delivery_mode").GetString());
        Assert.Equal("foreground", cli.ParseArguments(1).GetProperty("delivery_mode").GetString());
        Assert.Equal(ActionDelivery.Foreground, receipt.Delivery);
        Assert.Contains("escalated: background_unavailable", receipt.Warnings);
    }

    [Fact]
    public async Task Foreground_Retry_Does_Not_Fire_On_Other_Failures()
    {
        var cli = new FakeCuaCli().Enqueue(0, """{"error":"target not found"}""");
        var backend = new CuaCliBackend(cli, allowForegroundRetry: true);

        var receipt = await backend.ExecuteAsync(Target, new ClickAction(1, 2));

        Assert.Equal(ActionEffect.Failed, receipt.Effect);
        Assert.Single(cli.Calls);
    }

    [Fact]
    public void Capabilities_Advertise_What_The_Driver_Provides()
    {
        var backend = new CuaCliBackend(new FakeCuaCli());

        Assert.True(backend.Capabilities.HasFlag(BackendCapabilities.TargetDiscovery));
        Assert.True(backend.Capabilities.HasFlag(BackendCapabilities.Screenshot));
        Assert.True(backend.Capabilities.HasFlag(BackendCapabilities.AccessibilityTree));
        Assert.True(backend.Capabilities.HasFlag(BackendCapabilities.BackgroundClick));
        Assert.True(backend.Capabilities.HasFlag(BackendCapabilities.BackgroundTyping));
        Assert.True(backend.Capabilities.HasFlag(BackendCapabilities.ForegroundInput));
    }
}
