using Avalonia.Controls;
using FluentAssertions;
using NexusMods.App.UI.Controls.Spine.Buttons.Download;
using NexusMods.UI.Tests.Framework;

namespace NexusMods.UI.Tests.Controls.Spine;

public class DownloadButtonViewTests : AViewTest<DownloadButtonView, DownloadButtonDesignerViewModel, IDownloadButtonViewModel>
{
    public DownloadButtonViewTests(IServiceProvider provider, AvaloniaApp app) : base(provider, app) { }

    [Fact]
    public async Task SettingButtonToActiveAppliesProperClass()
    {
        var button = await Host.GetViewControl<Button>("ParentButton");

        ViewModel.IsActive = true;
        
        await Host.OnUi(async () =>
        {
            button.Classes.Should().Contain("Active");
        });

    }
}
