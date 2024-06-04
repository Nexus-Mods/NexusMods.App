using Avalonia.Controls;
using FluentAssertions;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.Updater;

namespace NexusMods.UI.Tests.Overlays;

public class UpdaterViewTests : AViewTest<UpdaterView, UpdaterDesignViewModel, IUpdaterViewModel>
{
    public UpdaterViewTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    public async Task ClickingUpdateCallsTheCommand()
    {
        var controller = new OverlayController();
        controller.Enqueue(ViewModel);

        var btn = await Host.GetViewControl<Button>("UpdateButton");
        await Click(btn);

        await Task.Delay(10000);

        await EventuallyOnUi(() =>
        {
            ViewModel.Status.Should().Be(Status.Closed);
            ViewModel.UpdateClicked.Should().BeTrue();
        });
    }

    [Fact]
    public async Task ShowChangelogIsWiredCorrectly()
    {
        var controller = new OverlayController();
        controller.Enqueue(ViewModel);

        var btn = await Host.GetViewControl<Button>("ChangelogButton");
        await Click(btn);

        await EventuallyOnUi(() =>
        {
            ViewModel.ChangelogShown.Should().BeTrue();
        });
    }

    [Fact]
    public async Task ClickingLaterClosesTheOverlay()
    {
        var controller = new OverlayController();
        controller.Enqueue(ViewModel);


        var btn = await Host.GetViewControl<Button>("LaterButton");
        await Click(btn);

        await EventuallyOnUi(() =>
        {
            ViewModel.Status.Should().Be(Status.Closed);
        });

    }
}
