using Backseat.Core;

namespace Backseat.Core.Tests;

public sealed class ObservationSettleTests
{
    private static readonly TargetDescriptor KnownTarget = new() { ProcessId = 42, WindowId = 99 };

    private static FakeBackend BackendWith(Func<int, Observation> observationFactory)
    {
        var backend = new FakeBackend();
        backend.Targets.Add(KnownTarget);
        backend.ObserveHandler = (_, _) => Task.FromResult(observationFactory(backend.ObserveCalls));
        return backend;
    }

    private static Observation Degraded() => new()
    {
        Target = KnownTarget,
        Timestamp = DateTimeOffset.UtcNow,
        IsDegraded = true,
        DegradedReason = "ax_tree_empty",
    };

    private static Observation Healthy() => new()
    {
        Target = KnownTarget,
        Timestamp = DateTimeOffset.UtcNow,
        AccessibilityTree = "- Window",
    };

    private static readonly ObservationSettlePolicy FastPolicy = new()
    {
        MaxAttempts = 3,
        Delay = TimeSpan.Zero,
    };

    [Fact]
    public async Task Degraded_Observations_Are_Retried_Until_They_Settle()
    {
        var backend = BackendWith(call => call >= 2 ? Healthy() : Degraded());
        var session = new Session(backend, settlePolicy: FastPolicy);
        await session.SelectTargetAsync(KnownTarget);

        var observation = await session.ObserveAsync();

        Assert.False(observation.IsDegraded);
        Assert.Equal(2, backend.ObserveCalls);
        Assert.Single(session.Observations);
    }

    [Fact]
    public async Task Retry_Stops_At_The_Attempt_Limit()
    {
        var backend = BackendWith(_ => Degraded());
        var session = new Session(backend, settlePolicy: FastPolicy);
        await session.SelectTargetAsync(KnownTarget);

        var observation = await session.ObserveAsync();

        Assert.True(observation.IsDegraded);
        Assert.Equal("ax_tree_empty", observation.DegradedReason);
        Assert.Equal(3, backend.ObserveCalls);
        Assert.Single(session.Observations);
    }

    [Fact]
    public async Task Settled_Observations_Do_Not_Retry()
    {
        var backend = BackendWith(_ => Healthy());
        var session = new Session(backend, settlePolicy: FastPolicy);
        await session.SelectTargetAsync(KnownTarget);

        await session.ObserveAsync();

        Assert.Equal(1, backend.ObserveCalls);
    }

    [Fact]
    public async Task Single_Attempt_Policy_Disables_Retries()
    {
        var backend = BackendWith(_ => Degraded());
        var session = new Session(backend, settlePolicy: new ObservationSettlePolicy { MaxAttempts = 1 });
        await session.SelectTargetAsync(KnownTarget);

        var observation = await session.ObserveAsync();

        Assert.True(observation.IsDegraded);
        Assert.Equal(1, backend.ObserveCalls);
    }

    [Fact]
    public void Policy_Rejects_Invalid_Limits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ObservationSettlePolicy { MaxAttempts = 0 }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new ObservationSettlePolicy { Delay = TimeSpan.FromMilliseconds(-1) }.Validate());
    }
}
