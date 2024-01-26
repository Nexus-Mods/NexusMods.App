using Avalonia.Controls;
using FluentAssertions;
using NexusMods.App.UI.Overlays.MetricsOptIn;

namespace NexusMods.UI.Tests.Overlays;

public class MetricsOptInViewTests : AViewTest<MetricsOptInView, MetricsOptInDesignerViewModel, IMetricsOptInViewModel>
{
    public MetricsOptInViewTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    public async Task ClickingAllowCallsTheAllowCommandAndClosesWindow()
    {
        ViewModel.IsActive = true;
        ViewModel.IsActive.Should().BeTrue();

        var btn = await Host.GetViewControl<Button>("AllowButton");
        await Click(btn);

        await Eventually(() =>
        {
            ViewModel.IsActive.Should().BeFalse();
            ViewModel.AllowClicked.Should().BeTrue();
        });
    }

    [Fact]
    public async Task ClickingDenyCallsTheDenyCommandAndClosesWindow()
    {
        ViewModel.IsActive = true;
        ViewModel.IsActive.Should().BeTrue();

        var btn = await Host.GetViewControl<Button>("DenyButton");
        await Click(btn);

        await Eventually(() =>
        {

            ViewModel.IsActive.Should().BeFalse();
            ViewModel.DenyClicked.Should().BeTrue();
        });
    }
}
