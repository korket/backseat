using Backseat.Core;

namespace Backseat.Core.Tests;

public sealed class SessionTests
{
    private static readonly TargetDescriptor KnownTarget = new()
    {
        ProcessId = 42,
        WindowId = 99,
        Title = "Untitled - Notepad",
    };

    private static FakeBackend BackendWith(TargetDescriptor target)
    {
        var backend = new FakeBackend();
        backend.Targets.Add(target);
        return backend;
    }

    [Fact]
    public async Task SelectTarget_Rejects_Targets_The_Backend_Does_Not_Report()
    {
        var session = new Session(new FakeBackend());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => session.SelectTargetAsync(new TargetDescriptor { ProcessId = 7 }));

        Assert.Contains("was not reported", exception.Message);
        Assert.Equal(SessionState.Created, session.State);
        Assert.Null(session.Target);
    }

    [Fact]
    public async Task SelectTarget_Uses_The_Backend_Descriptor()
    {
        var session = new Session(BackendWith(KnownTarget));

        var selected = await session.SelectTargetAsync(new TargetDescriptor { ProcessId = 42, WindowId = 99 });

        Assert.Equal("Untitled - Notepad", selected.Title);
        Assert.Equal(SessionState.TargetSelected, session.State);
        Assert.Equal(selected, session.Target);
    }

    [Fact]
    public async Task SelectTarget_Rejects_A_Second_Target()
    {
        var session = new Session(BackendWith(KnownTarget));
        await session.SelectTargetAsync(KnownTarget);

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.SelectTargetAsync(KnownTarget));
    }

    [Fact]
    public async Task Observe_Requires_A_Selected_Target()
    {
        var session = new Session(new FakeBackend());

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.ObserveAsync());
    }

    [Fact]
    public async Task Observe_Transitions_To_Active_And_Records_The_Observation()
    {
        var session = new Session(BackendWith(KnownTarget));
        await session.SelectTargetAsync(KnownTarget);

        var observation = await session.ObserveAsync();

        Assert.Equal(SessionState.Active, session.State);
        Assert.Single(session.Observations);
        Assert.Equal(observation, session.Observations[0]);
    }

    [Fact]
    public async Task Execute_Assigns_Increasing_Sequence_Numbers()
    {
        var session = new Session(BackendWith(KnownTarget));
        await session.SelectTargetAsync(KnownTarget);

        await session.ExecuteAsync(new ClickAction(1, 2));
        await session.ExecuteAsync(new PressKeyAction("return"));

        Assert.Equal(2, session.Actions.Count);
        Assert.Equal(1, session.Actions[0].Sequence);
        Assert.Equal(2, session.Actions[1].Sequence);
        Assert.IsType<ClickAction>(session.Actions[0].Action);
        Assert.IsType<PressKeyAction>(session.Actions[1].Action);
        Assert.Equal(SessionState.Active, session.State);
    }

    [Fact]
    public async Task Execute_Preserves_Failed_Receipts_Verbatim()
    {
        var backend = BackendWith(KnownTarget);
        backend.ExecuteHandler = (_, _) => Task.FromResult(new ActionReceipt
        {
            Effect = ActionEffect.Failed,
            Error = "cua-driver click exited with code 1: daemon gone",
        });

        var session = new Session(backend);
        await session.SelectTargetAsync(KnownTarget);

        var receipt = await session.ExecuteAsync(new ClickAction(1, 2));

        Assert.Equal(ActionEffect.Failed, receipt.Effect);
        Assert.Contains("daemon gone", session.Actions[0].Receipt.Error);
    }

    [Fact]
    public async Task Execute_Preserves_Non_Background_Safe_Receipts()
    {
        var backend = BackendWith(KnownTarget);
        backend.ExecuteHandler = (_, _) => Task.FromResult(new ActionReceipt
        {
            Effect = ActionEffect.Confirmed,
            Delivery = ActionDelivery.Foreground,
            DeliveryRoute = "global_input",
        });

        var session = new Session(backend);
        await session.SelectTargetAsync(KnownTarget);

        var exception = await Assert.ThrowsAsync<DeliveryPolicyViolationException>(
            () => session.ExecuteAsync(new ClickAction(1, 2)));

        Assert.False(exception.Receipt.ConfirmsBackgroundSafe);
        Assert.Equal(ActionDelivery.Foreground, exception.Receipt.Delivery);
        Assert.Equal(ActionDelivery.Foreground, session.Actions[0].Receipt.Delivery);
    }

    [Fact]
    public async Task Close_Cancels_The_Lifetime_Token_And_Disposes_The_Backend()
    {
        var backend = BackendWith(KnownTarget);
        var session = new Session(backend);
        await session.SelectTargetAsync(KnownTarget);

        await session.CloseAsync();

        Assert.Equal(SessionState.Closed, session.State);
        Assert.True(session.CancellationToken.IsCancellationRequested);
        Assert.True(backend.Disposed);
    }

    [Fact]
    public async Task Operations_After_Close_Throw()
    {
        var session = new Session(BackendWith(KnownTarget));
        await session.SelectTargetAsync(KnownTarget);
        await session.CloseAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ObserveAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ExecuteAsync(new ClickAction(1, 2)));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.SelectTargetAsync(KnownTarget));
    }

    [Fact]
    public async Task InFlight_Operations_Observe_Cancellation()
    {
        var backend = BackendWith(KnownTarget);
        backend.ExecuteHandler = async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return new ActionReceipt { Effect = ActionEffect.Confirmed };
        };

        var session = new Session(backend);
        await session.SelectTargetAsync(KnownTarget);

        var pending = session.ExecuteAsync(new ClickAction(1, 2));
        await session.CloseAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(SessionState.Closed, session.State);
        Assert.Empty(session.Actions);
    }

    [Fact]
    public async Task Close_Is_Idempotent()
    {
        var session = new Session(BackendWith(KnownTarget));

        await session.CloseAsync();
        await session.CloseAsync();

        Assert.Equal(SessionState.Closed, session.State);
    }

    [Fact]
    public async Task Dispose_Closes_The_Session()
    {
        var backend = BackendWith(KnownTarget);

        await using (var session = new Session(backend))
        {
            await session.SelectTargetAsync(KnownTarget);
            Assert.Equal(SessionState.TargetSelected, session.State);
        }

        Assert.True(backend.Disposed);
    }

    [Fact]
    public void Session_Has_Stable_Identity()
    {
        var session = new Session(new FakeBackend());

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.NotEqual(default, session.CreatedAt);
        Assert.Equal(session.Id, session.Id);
    }
}
