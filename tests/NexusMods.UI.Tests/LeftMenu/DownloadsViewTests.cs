using Avalonia.Controls;
using FluentAssertions;
using FomodInstaller.Interface.ui;
using Microsoft.Extensions.Hosting;
using NexusMods.App.UI.LeftMenu.Downloads;
using NexusMods.UI.Tests.Framework;

namespace NexusMods.UI.Tests.LeftMenu;

public class DownloadsViewTests : IAsyncLifetime
{
    private readonly AvaloniaApp _app;
    private ControlHost<DownloadsView,DownloadsDesignViewModel,IDownloadsViewModel> _host;
    private (Options Option, Button Control)[] _options;

    public DownloadsViewTests(AvaloniaApp app)
    {
        _app = app;
    }

    [Fact]
    public async Task ActiveIsAppliedToButtonsWhenTheVMActivatesThem()
    {
        /// Select each option in turn
        foreach (var current in _options)
        {
            _host.ViewModel.Set(current.Option);
            _host.ViewModel.Current.Should().Be(current.Option, "value was set in the View Model");
            await _host.Flush();

            /// Check each of the controls for their correct active state
            foreach (var checking in _options)
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

    [Fact]
    public async Task ClickingAButtonMakesItActive()
    {
        foreach (var option in _options)
        {
            option.Control.Command!.Execute(null);
            await _host.Flush();

        }

    }

    public async Task InitializeAsync()
    {
        _host =
            await _app.GetControl<DownloadsView, DownloadsDesignViewModel,
                IDownloadsViewModel>();

        var inProgress = await _host.GetViewControl<Button>("InProgressButton");
        var completed = await _host.GetViewControl<Button>("CompletedButton");
        var history = await _host.GetViewControl<Button>("HistoryButton");

        _options = new (Options Option, Button Control)[]
        {
            (Options.InProgress, inProgress),
            (Options.Completed, completed),
            (Options.History, history)
        };
    }

    public async Task DisposeAsync()
    {
        await _host.DisposeAsync();
    }
}
