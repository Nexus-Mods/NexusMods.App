using Avalonia.Controls;
using FluentAssertions;
using NexusMods.App.UI.Overlays.Updater;
using NexusMods.Common;

namespace NexusMods.UI.Tests.Overlays;

public class UpdaterViewTests : AViewTest<UpdaterView, UpdaterDesignViewModel, IUpdaterViewModel>
{
    public UpdaterViewTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    public async Task ClickingUpdateCallsTheCommand()
    {

        ViewModel.IsActive = true;
        ViewModel.IsActive.Should().BeTrue();

        var btn = await Host.GetViewControl<Button>("UpdateButton");
        await Click(btn);

        await Task.Delay(10000);

        await EventuallyOnUi(() =>
        {
            ViewModel.IsActive.Should().BeFalse();
            ViewModel.UpdateClicked.Should().BeTrue();
        });
    }

    [Fact]
    public async Task UsingFlatpakDisablesUpdateButtonAndShowsMessage()
    {
        ViewModel.Method = InstallationMethod.Flatpak;

        var useSystemUpdater = await Host.GetViewControl<TextBlock>("UseSystemUpdater");
        var updateButton = await Host.GetViewControl<Button>("UpdateButton");

        await EventuallyOnUi(() =>
        {
            useSystemUpdater.IsVisible.Should().BeTrue();
            updateButton.IsEnabled.Should().BeFalse();
        });
    }

    [Fact]
    public async Task ShowChangelogIsWiredCorrectly()
    {
        var btn = await Host.GetViewControl<Button>("ChangelogButton");
        await Click(btn);

        await EventuallyOnUi(() =>
        {
            ViewModel.ChangelogShown.Should().BeTrue();
        });
    }
}
