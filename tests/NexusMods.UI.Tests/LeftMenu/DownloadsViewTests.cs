using Avalonia.Controls;
using FluentAssertions;
using FomodInstaller.Interface.ui;
using Microsoft.Extensions.Hosting;
using NexusMods.App.UI.LeftMenu.Downloads;
using NexusMods.UI.Tests.Framework;

namespace NexusMods.UI.Tests.LeftMenu;

public class DownloadsViewTests
{
    private readonly AvaloniaApp _app;

    public DownloadsViewTests(AvaloniaApp app)
    {
        _app = app;
    }

    [Fact]
    public async Task ActiveIsAppliedToButtonsWhenTheVMActivatesThem()
    {
        await using var host =
            await _app.GetControl<DownloadsView, DownloadsDesignViewModel,
                IDownloadsViewModel>();

        var inProgress = await host.GetViewControl<Button>("InProgressButton");
        var completed = await host.GetViewControl<Button>("CompletedButton");
        var history = await host.GetViewControl<Button>("HistoryButton");

        var options = new (Options Option, Button Control)[]
        {
            (Options.InProgress, inProgress),
            (Options.Completed, completed),
            (Options.History, history)
        };

        /// Select each option in turn
        foreach (var current in options)
        {
            host.ViewModel.Set(current.Option);
            host.ViewModel.Current.Should().Be(current.Option, "value was set in the View Model");
            await host.Flush();

            /// Check each of the controls for their correct active state
            foreach (var checking in options)
            {
                if (checking.Option == current.Option)
                {
                    checking.Control.Classes.Should().Contain("Active",
                        $"this is {checking.Option} and {current.Option} is active");
                }
                else
                {
                    checking.Control.Classes.Should().NotContain("Active",
                        $"this is {checking.Option} and {current.Option} is active");
                }
            }
        }
    }

}
