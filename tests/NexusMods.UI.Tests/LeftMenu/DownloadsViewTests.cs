using FluentAssertions;
using NexusMods.App.UI.LeftMenu.Downloads;
using NexusMods.App.UI.RightContent.Downloads;
using Button = Avalonia.Controls.Button;

namespace NexusMods.UI.Tests.LeftMenu;

public class DownloadsViewTests : AViewTest<DownloadsView, DownloadsDesignViewModel, IDownloadsViewModel>
{
    private (Options Option, Button Control, Type Type)[]? _options;
    
    public DownloadsViewTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    public async Task ActiveIsAppliedToButtonsWhenTheVmActivatesThem()
    {
        // Select each option in turn
        foreach (var current in _options!)
        {
            Host.ViewModel.Set(current.Option);
            await EventuallyOnUi(() =>
            {
                Host.ViewModel.Current.Should().Be(current.Option, "value was set in the View Model");

                // Check each of the controls for their correct active state
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
            });
        }
    }

    [Fact]
    public async Task ClickingAButtonMakesItActive()
    {
        foreach (var option in _options!)
        {
            await Click(option.Control);
            Host.ViewModel.Current.Should().Be(option.Option, "value was set in the View Model");
            Host.ViewModel.CurrentViewModel.Should().NotBeNull("the view model should be set");
            Host.ViewModel.CurrentViewModel.Should().BeAssignableTo(option.Type, "the view model should be the correct type");
        }

    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var inProgress = await Host.GetViewControl<Button>("InProgressButton");
        var completed = await Host.GetViewControl<Button>("CompletedButton");
        var history = await Host.GetViewControl<Button>("HistoryButton");

        _options = new (Options Option, Button Control, Type Type)[]
        {
            (Options.InProgress, inProgress, typeof(IInProgressViewModel)),
            (Options.Completed, completed, typeof(ICompletedViewModel)),
            (Options.History, history, typeof(IHistoryViewModel))
        };
    }
}
