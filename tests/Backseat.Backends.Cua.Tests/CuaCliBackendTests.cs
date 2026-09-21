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
