using Avalonia.Controls;
using FluentAssertions;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.MetricsOptIn;

namespace NexusMods.UI.Tests.Overlays;

public class MetricsOptInViewTests : AViewTest<MetricsOptInView, MetricsOptInDesignerViewModel, IMetricsOptInViewModel>
{
    public MetricsOptInViewTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    public async Task ClickingAllowCallsTheAllowCommandAndClosesWindow()
    {
        var controller = new OverlayController();
        controller.Enqueue(ViewModel);
        ViewModel.Status.Should().Be(Status.Visible);

        var btn = await Host.GetViewControl<Button>("AllowButton");
        await Click(btn);

        await Eventually(() =>
        { 
            ViewModel.Status.Should().Be(Status.Closed);
            ViewModel.AllowClicked.Should().BeTrue();
        });
    }

    [Fact]
    public async Task ClickingDenyCallsTheDenyCommandAndClosesWindow()
    {
        var controller = new OverlayController();
        controller.Enqueue(ViewModel);
        ViewModel.Status.Should().Be(Status.Visible);

        var btn = await Host.GetViewControl<Button>("DenyButton");
        await Click(btn);

        await Eventually(() =>
        {

            ViewModel.Status.Should().Be(Status.Closed);
            ViewModel.DenyClicked.Should().BeTrue();
        });
    }
}
