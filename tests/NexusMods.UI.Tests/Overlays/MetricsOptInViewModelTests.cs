using FluentAssertions;
using NexusMods.App.UI.Overlays.MetricsOptIn;

namespace NexusMods.UI.Tests.Overlays;

public class MetricsOptInViewModelTests : AVmTest<IMetricsOptInViewModel>
{
    public MetricsOptInViewModelTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    public async Task CanOptInAndOut()
    {

        GlobalSettingsManager.GetMetricsOptIn().Should().BeFalse();

        Vm.Allow.Execute(null);

        await Eventually(() =>
        {
            GlobalSettingsManager.GetMetricsOptIn().Should().BeTrue();
        });

        Vm.Deny.Execute(null);

        await Eventually(() =>
        {
            GlobalSettingsManager.GetMetricsOptIn().Should().BeFalse();
        });
    }
}
