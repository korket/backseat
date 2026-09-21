using Backseat.Core;

namespace Backseat.Core.Tests;

public sealed class BackendContractTests
{
    [Fact]
    public async Task StubBackend_Implements_The_Contract_Without_BackendTypes()
    {
        IComputerBackend backend = new StubBackend();

        Assert.Equal(BackendCapabilities.TargetDiscovery | BackendCapabilities.Screenshot, backend.Capabilities);

        var targets = await backend.DiscoverTargetsAsync();
        var target = Assert.Single(targets);
        Assert.Equal(4242u, target.ProcessId);

        var observation = await backend.ObserveAsync(target);
        Assert.Equal(target, observation.Target);
        Assert.NotNull(observation.ScreenshotPng);

        var receipt = await backend.ExecuteAsync(target, new ClickAction(1, 2));
        Assert.Equal(ActionEffect.Unverifiable, receipt.Effect);
        Assert.False(receipt.ConfirmsBackgroundSafe);
    }

    private sealed class StubBackend : IComputerBackend
    {
        public string Name => "stub";

        public BackendCapabilities Capabilities =>
            BackendCapabilities.TargetDiscovery | BackendCapabilities.Screenshot;

        public Task<IReadOnlyList<TargetDescriptor>> DiscoverTargetsAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TargetDescriptor> targets = new[] { new TargetDescriptor { ProcessId = 4242 } };
            return Task.FromResult(targets);
        }

        public Task<Observation> ObserveAsync(TargetDescriptor target, CancellationToken cancellationToken = default)
        {
            var observation = new Observation
            {
                Target = target,
                Timestamp = DateTimeOffset.UnixEpoch,
                ScreenshotPng = new byte[] { 1, 2, 3 },
            };

            return Task.FromResult(observation);
        }

        public Task<ActionReceipt> ExecuteAsync(TargetDescriptor target, ComputerAction action, CancellationToken cancellationToken = default)
        {
            var receipt = new ActionReceipt { Effect = ActionEffect.Unverifiable };
            return Task.FromResult(receipt);
        }
    }
}
