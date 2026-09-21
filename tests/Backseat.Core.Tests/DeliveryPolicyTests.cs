using Backseat.Core;

namespace Backseat.Core.Tests;

public sealed class DeliveryPolicyTests
{
    private static readonly TargetDescriptor KnownTarget = new() { ProcessId = 42, WindowId = 99 };

    private static FakeBackend BackendReturning(ActionReceipt receipt)
    {
        var backend = new FakeBackend();
        backend.Targets.Add(KnownTarget);
        backend.ExecuteHandler = (_, _) => Task.FromResult(receipt);
        return backend;
    }

    private static ActionReceipt ForegroundReceipt() => new()
    {
        Effect = ActionEffect.Confirmed,
        Delivery = ActionDelivery.Foreground,
        DeliveryRoute = "global_input",
    };

    [Fact]
    public async Task BackgroundOnly_Rejects_Foreground_Receipts_Loudly()
    {
        var backend = BackendReturning(ForegroundReceipt());
        var session = new Session(backend);
        await session.SelectTargetAsync(KnownTarget);

        var exception = await Assert.ThrowsAsync<DeliveryPolicyViolationException>(
            () => session.ExecuteAsync(new ClickAction(1, 2)));

        Assert.Equal(ActionDelivery.Foreground, exception.Receipt.Delivery);
        Assert.Contains("BackgroundOnly", exception.Message);
    }

    [Fact]
    public async Task BackgroundOnly_Still_Records_The_Violating_Action()
    {
        var backend = BackendReturning(ForegroundReceipt());
        var session = new Session(backend);
        await session.SelectTargetAsync(KnownTarget);

        await Assert.ThrowsAsync<DeliveryPolicyViolationException>(() => session.ExecuteAsync(new ClickAction(1, 2)));

        var record = Assert.Single(session.Actions);
        Assert.Equal(ActionDelivery.Foreground, record.Receipt.Delivery);
        Assert.False(record.Receipt.ConfirmsBackgroundSafe);
    }

    [Fact]
    public async Task AllowForeground_Accepts_Foreground_Receipts()
    {
        var backend = BackendReturning(ForegroundReceipt());
        var session = new Session(backend, deliveryPolicy: DeliveryPolicy.AllowForeground);
        await session.SelectTargetAsync(KnownTarget);

        var receipt = await session.ExecuteAsync(new ClickAction(1, 2));

        Assert.Equal(ActionDelivery.Foreground, receipt.Delivery);
        Assert.Single(session.Actions);
    }

    [Fact]
    public async Task Background_Receipts_Pass_Under_Both_Policies()
    {
        var receipt = new ActionReceipt
        {
            Effect = ActionEffect.Confirmed,
            Delivery = ActionDelivery.Background,
            DeliveryRoute = "accessibility",
        };

        var strict = new Session(BackendReturning(receipt));
        await strict.SelectTargetAsync(KnownTarget);
        Assert.True((await strict.ExecuteAsync(new ClickAction(1, 2))).ConfirmsBackgroundSafe);

        var permissive = new Session(BackendReturning(receipt), deliveryPolicy: DeliveryPolicy.AllowForeground);
        await permissive.SelectTargetAsync(KnownTarget);
        Assert.True((await permissive.ExecuteAsync(new ClickAction(1, 2))).ConfirmsBackgroundSafe);
    }

    [Fact]
    public async Task Default_Policy_Is_BackgroundOnly()
    {
        var session = new Session(new FakeBackend());

        Assert.Equal(DeliveryPolicy.BackgroundOnly, session.DeliveryPolicy);
        await session.CloseAsync();
    }
}
