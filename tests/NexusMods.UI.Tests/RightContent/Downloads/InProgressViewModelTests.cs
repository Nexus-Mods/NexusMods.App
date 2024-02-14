using System.Reactive;
using FluentAssertions;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Pages.Downloads;
using NexusMods.App.UI.Pages.Downloads.ViewModels;
using NexusMods.Networking.Downloaders.Interfaces;
using Noggog;
using ReactiveUI;

namespace NexusMods.UI.Tests.RightContent.Downloads;

public class InProgressViewModelTests : AViewTest<InProgressView, InProgressDesignViewModel, IInProgressViewModel>
{
    public InProgressViewModelTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    public async Task UpdatingViewmodelTasks_UpdatesTheDisplayedItems()
    {
        // Assert Default Empty State
        var view = Host.View;
        await OnUi(() =>
        {
            // Assigned upon view activation. No setter for this item, so no need to test swap.
            view.ModsDataGrid.ItemsSource.Any().Should().BeTrue();
        });
    }

    [Fact]
    public async Task? GridHasCorrectColumnCount()
    {
        // Assert Default Empty State
        var view = Host.View;
        await OnUi(() =>
        {
            // Assigned upon view activation. Only set once.
            view.ModsDataGrid.Columns.Count.Should().Be(5);
        });
    }

    [Fact]
    public async Task AddingDownloads_UpdatesTheDatagrid()
    {
        await OnUi(() =>
        {
            // Calculate Current Item Count
            var currentCount = View.ModsDataGrid.ItemsSource.Cast<object>().Count();

            // Add an item.
            var download = new DownloadTaskDesignViewModel();
            Host.ViewModel.AddDownload(download);

            // Because this runs on the UI thread, results should be instant, provided some deliberate cross
            // thread shenanigans don't happen under the hood.

            // Assert a new item has arrived.
            var newCount = View.ModsDataGrid.ItemsSource.Cast<object>().Count();
            newCount.Should().Be(currentCount + 1);
        });
    }

    [Fact]
    public async Task ClickingCancel_FiresTheCommand()
    {
        await OnUi(() =>
        {
            var source = new TaskCompletionSource<bool>();
            ViewModel.ShowCancelDialogCommand = ReactiveCommand.Create<Unit, Unit>(_ =>
            {
                source.SetResult(true);
                return Unit.Default;
            });

            // Assert Cancel Command
            View.CancelButton.Command.Should().Be(ViewModel.ShowCancelDialogCommand);

            // Click and Ensure it Fired
            Click_AlreadyOnUi(View.CancelButton);
            source.Task.Result.Should().BeTrue();
        });
    }

    [Fact]
    public async Task IsRunning_False_NoDownloadsArePresent()
    {
        // Bindings happen on UI, so we need to run on UI.
        await OnUi(() =>
        {
            ViewModel.ClearDownloads();
            ViewModel.HasDownloads.Should().BeFalse();
        });
    }

    [Fact]
    public async Task IsRunning_True_WhenRunningDownloadIsPresent()
    {
        await OnUi(() =>
        {
            ViewModel.ClearDownloads();
            ViewModel.HasDownloads.Should().BeFalse();

            ViewModel.AddDownload(new DownloadTaskDesignViewModel()
            {
                Status = DownloadTaskStatus.Downloading
            });
            ViewModel.HasDownloads.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Styles_AreUpdated_WhenDownloadsAreRunning()
    {
        await OnUi(() =>
        {
            ViewModel.ClearDownloads();
            ViewModel.HasDownloads.Should().BeFalse();

            // No downloads are running
            View.InProgressTitleCountTextBlock.Classes.Should().Contain("ForegroundWeak");
            View.NoDownloadsTextBlock.IsVisible.Should().Be(true);

            // Check the title is correct with 0 elements.
            View.InProgressTitleCountTextBlock.Text.Should().Be(StringFormatters.ToDownloadsInProgressTitle(0));

            // Now let's add an element.
            ViewModel.AddDownload(new DownloadTaskDesignViewModel()
            {
                Status = DownloadTaskStatus.Downloading
            });

            // Count color should no longer be weak, and the no downloads text should be hidden.
            View.InProgressTitleCountTextBlock.Classes.Should().NotContain("ForegroundWeak");
            View.NoDownloadsTextBlock.IsVisible.Should().Be(false);

            // Check the title is correct with 0 elements.
            View.InProgressTitleCountTextBlock.Text.Should().Be(StringFormatters.ToDownloadsInProgressTitle(1));
        });
    }

    [Fact]
    public async Task Title_IsUpdated_WhenCollectionChanges()
    {
        await OnUi(() =>
        {
            ViewModel.ClearDownloads();
            ViewModel.HasDownloads.Should().BeFalse();

            // Check the title is correct with 0 elements.
            View.InProgressTitleCountTextBlock.Text.Should().Be(StringFormatters.ToDownloadsInProgressTitle(0));

            // Now let's add an element.
            ViewModel.AddDownload(new DownloadTaskDesignViewModel
            {
                Status = DownloadTaskStatus.Downloading
            });
            View.InProgressTitleCountTextBlock.Text.Should().Be(StringFormatters.ToDownloadsInProgressTitle(1));
        });
    }

    [Fact]
    public async Task DownloadedBytesText_IsUpdated_WhenCollectionChanges()
    {
        await OnUi(() =>
        {
            ViewModel.ClearDownloads();
            ViewModel.HasDownloads.Should().BeFalse();

            // Check the total completion is correct with 0 elements.
            View.SizeCompletionTextBlock.Text.Should().Be(StringFormatters.ToSizeString(0, 0));

            // Check the total completion is correct with 1 element.
            ViewModel.AddDownload(new DownloadTaskDesignViewModel()
            {
                Status = DownloadTaskStatus.Downloading,
                DownloadedBytes = 1337,
                SizeBytes = 4200
            });

            View.SizeCompletionTextBlock.Text.Should().Be(StringFormatters.ToSizeString(1337, 4200));
        });
    }

    [Fact]
    public async Task DownloadProgressBar_IsUpdated_WhenCollectionChanges()
    {
        await OnUi(() =>
        {
            ViewModel.ClearDownloads();
            ViewModel.HasDownloads.Should().BeFalse();

            // Check the total completion is correct with 0 elements.
            View.DownloadProgressBar.Value.Should().Be(0);

            // Check the total completion is correct with 1 element.
            ViewModel.AddDownload(new DownloadTaskDesignViewModel()
            {
                Status = DownloadTaskStatus.Downloading,
                DownloadedBytes = 1000,
                SizeBytes = 4000
            });

            View.DownloadProgressBar.Value.Should().Be(0.25);
        });
    }

    [Fact]
    public async Task TimeRemainingText_IsUpdated_WhenCollectionChanges()
    {
        await OnUi(() =>
        {
            ViewModel.ClearDownloads();
            ViewModel.HasDownloads.Should().BeFalse();

            // Check the total completion is correct with 0 elements.
            var originalTimeRemaining = StringFormatters.ToTimeRemainingShort(ViewModel.SecondsRemaining);
            // If 0 seconds remaining, then no download is occuring, and the text block is empty string
            if (ViewModel.SecondsRemaining == 0)
            {
                View.BoldMinutesRemainingTextBlock.Text.Should().Be("");
            }
            else
            {
                View.BoldMinutesRemainingTextBlock.Text.Should().Be(originalTimeRemaining);
            }

            // Check the total completion is correct with 1 element.
            ViewModel.AddDownload(new DownloadTaskDesignViewModel()
            {
                Status = DownloadTaskStatus.Downloading,
                DownloadedBytes = 1337,
                SizeBytes = 4200,
                Throughput = 1000
            });

            var newTimeRemaining = StringFormatters.ToTimeRemainingShort(ViewModel.SecondsRemaining);
            newTimeRemaining.Should().NotBe(originalTimeRemaining);
            View.BoldMinutesRemainingTextBlock.Text.Should().Be(newTimeRemaining);
        });
    }

    [Fact]
    public async Task SizeBytes_IsUpdated_WhenItemAddedToCollection()
    {
        await OnUi(() =>
        {
            ViewModel.ClearDownloads();
            ViewModel.DownloadedSizeBytes.Should().Be(0);
            ViewModel.TotalSizeBytes.Should().Be(0);

            // Run callback which should update property.
            ViewModel.AddDownload(new DownloadTaskDesignViewModel()
            {
                Status = DownloadTaskStatus.Downloading,
                DownloadedBytes = 1337,
                SizeBytes = 4200
            });

            ViewModel.DownloadedSizeBytes.Should().Be(1337);
            ViewModel.TotalSizeBytes.Should().Be(4200);
        });
    }

    [Fact]
    public async Task SizeBytes_IsUpdated_WhenItemValueChanges()
    {
        await OnUi(async () =>
        {
            ViewModel.ClearDownloads();
            ViewModel.DownloadedSizeBytes.Should().Be(0);
            ViewModel.TotalSizeBytes.Should().Be(0);

            var vm = new DownloadTaskDesignViewModel()
            {
                Status = DownloadTaskStatus.Downloading,
                DownloadedBytes = 0,
                SizeBytes = 0
            };

            // Add the data.
            ViewModel.AddDownload(vm);
            ViewModel.DownloadedSizeBytes.Should().Be(0);
            ViewModel.TotalSizeBytes.Should().Be(0);

            // Update the Data, and wait for Poll
            vm.DownloadedBytes = 1337;
            vm.SizeBytes = 4200;
            await Task.Delay((int)(InProgressViewModel.PollTimeMilliseconds * 1.5));

            // Assert the data has been updated.
            ViewModel.DownloadedSizeBytes.Should().Be(1337);
            ViewModel.TotalSizeBytes.Should().Be(4200);
        });
    }
}
