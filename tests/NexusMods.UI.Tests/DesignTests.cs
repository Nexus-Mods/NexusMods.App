using Avalonia.Controls;
using Avalonia.Headless;
using FluentAssertions;
using NexusMods.App.UI.Pages.DebugControls;

namespace NexusMods.UI.Tests;

public class DesignTests : AUiTest
{
    public DesignTests(IServiceProvider provider) : base(provider) {}

    [Fact]
    public async Task Test_Icons() => await Test(page => page.TabIcons);

    [Fact]
    public async Task Test_Colors() => await Test(page => page.TabColors);

    // [Fact]
    // public async Task Test_Controls() => await Test(page => page.TabControls);
    //
    // [Fact]
    // public async Task Test_Typography() => await Test(page => page.TabTypography);

    private async Task Test(Func<DebugControlsPageView, TabItem> getTab)
    {
        var window = await App.CreateWindow();

        string parameter = "";
        var ms = new MemoryStream();

        await App.OnUI(() =>
        {
            var control = new DebugControlsPageView();
            var tab = getTab(control);
            tab.IsSelected = true;
            parameter = tab.Name ?? string.Empty;

            window.Content = control;

            var frame = window.CaptureRenderedFrame();
            frame.Should().NotBeNull();

            frame!.Save(ms, quality: 80);
        });

        ms.Position.Should().NotBe(0);
        ms.Seek(0, SeekOrigin.Begin);

        await Verify(ms, extension: "png").UseParameters(parameter).DisableDiff();
    }
}
