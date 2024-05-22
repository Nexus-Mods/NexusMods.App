using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Pages.Downloads.ViewModels;
using NexusMods.App.UI.Resources;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Downloads;

public partial class InProgressView : ReactiveUserControl<IInProgressViewModel>
{
    public InProgressView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            CompletedDataGrid.Width = double.NaN;
            
            this.OneWayBind(ViewModel, vm => vm.Series, view => view.Chart.Series)
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.YAxes, view => view.Chart.YAxes)
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.XAxes, view => view.Chart.XAxes)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.ShowCancelDialogCommand, view => view.CancelButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.SuspendSelectedTasksCommand, view => view.PauseButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.SuspendAllTasksCommand, view => view.PauseAllButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.ResumeSelectedTasksCommand, view => view.ResumeButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.ResumeAllTasksCommand, view => view.ResumeAllButton)
                .DisposeWith(d);
            
            this.BindCommand(ViewModel, vm => vm.HideSelectedCommand, view => view.ClearButton)
                .DisposeWith(d);
            
            this.BindCommand(ViewModel, vm => vm.HideAllCommand, view => view.ClearAllButton)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.InProgressTasks, 
                    view => view.InprogressDataGrid.ItemsSource)
                .DisposeWith(d);
            
            this.OneWayBind(ViewModel, vm => vm.CompletedTasks, 
                    view => view.CompletedDataGrid.ItemsSource)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Columns)
                .GenerateColumns(InprogressDataGrid)
                .DisposeWith(d);

            // Fix the CompletedDataGrid Width when number of items changes
            this.WhenAnyValue(view => view.ViewModel!.CompletedTasks.Count)
                .Subscribe(count =>
                    {
                        CompletedDataGrid.Width = double.NaN;
                    }
                ).DisposeWith(d);

            // Dynamically hide the "No Downloads" TextBlock
            this.WhenAnyValue(view =>  view.ViewModel!.HasDownloads)
                .Select(hasDownloads => !hasDownloads)
                .BindToView(this, view => view.NoDownloadsTextBlock.IsVisible)
                .DisposeWith(d);

            // Dynamically Update Download Count
            this.WhenAnyValue(view => view.ViewModel!.ActiveDownloadCount)
                .Subscribe(count =>
                {
                    InProgressTitleCountTextBlock.Text = StringFormatters.ToDownloadsInProgressTitle(count);
                })
                .DisposeWith(d);

            // Dynamically Update Download Count color
            this.WhenAnyValue(view => view.ViewModel!.ActiveDownloadCount)
                .Select(count => count > 0)
                .BindToClasses(InProgressTitleCountTextBlock, "ForegroundStrong", "ForegroundWeak")
                .DisposeWith(d);

            // Dynamically Update Downloaded Bytes Text
            this.WhenAnyValue(view => view.ViewModel!.DownloadedSizeBytes, view => view.ViewModel!.TotalSizeBytes)
                .Subscribe(_ =>
                {
                    var vm = ViewModel!;
                    SizeCompletionTextBlock.Text =
                        StringFormatters.ToSizeString(vm.DownloadedSizeBytes, vm.TotalSizeBytes);
                    SizeCompletionTextBlock.IsVisible = vm.TotalSizeBytes > 0;


                    DownloadProgressBar.Value = vm.DownloadedSizeBytes / Math.Max(1.0, vm.TotalSizeBytes);
                    DownloadProgressBar.IsVisible = DownloadProgressBar.Value > 0;
                })
                .DisposeWith(d);

            // Dynamically Update Time Remaining Text
            this.WhenAnyValue(view => view.ViewModel!.SecondsRemaining)
                .Subscribe(_ =>
                {
                    var vm = ViewModel!;
                    if (vm.SecondsRemaining == 0)
                    {
                        BoldMinutesRemainingTextBlock.Text = "";
                        MinutesRemainingTextBlock.Text = "";
                    }
                    else
                    {
                        BoldMinutesRemainingTextBlock.Text = StringFormatters.ToTimeRemainingShort(vm.SecondsRemaining);
                        MinutesRemainingTextBlock.Text = Language.InProgressView_InProgressView_Remaining;
                    }
                })
                .DisposeWith(d);
            
            // Dynamically Update Completed Download Count
            this.WhenAnyValue(view => view.ViewModel!.CompletedTasks.Count)
                .Subscribe(count =>
                {
                    CompletedTitleCountTextBlock.Text = StringFormatters.ToDownloadsInProgressTitle(count);
                })
                .DisposeWith(d);
            
            // Hide Completed Section if no downloads
            this.WhenAnyValue(view => view.ViewModel!.CompletedTasks.Count)
                .Select(count => count > 0)
                .BindToView(this, view => view.CompletedSectionGrid.IsVisible)
                .DisposeWith(d);

            // Bind inProgress Selected Items
            Observable.FromEventPattern<SelectionChangedEventArgs>(
                    addHandler => InprogressDataGrid.SelectionChanged += addHandler,
                    removeHandler => InprogressDataGrid.SelectionChanged -= removeHandler)
                .Do(_ =>
                {
                    ViewModel!.SelectedInProgressTasks.Edit(updater =>
                    {
                        updater.Clear();
                        foreach (var item in InprogressDataGrid.SelectedItems)
                        {
                            if (item is IDownloadTaskViewModel task)
                                updater.Add(task);
                        }
                    });
                })
                .Subscribe()
                .DisposeWith(d);
            
            // Bind completed Selected Items
            Observable.FromEventPattern<SelectionChangedEventArgs>(
                addHandler => CompletedDataGrid.SelectionChanged += addHandler,
                removeHandler => CompletedDataGrid.SelectionChanged -= removeHandler)
                .Do(_ =>
                {
                    ViewModel!.SelectedCompletedTasks.Edit(updater =>
                    {
                        updater.Clear();
                        foreach (var item in CompletedDataGrid.SelectedItems)
                        {
                            if (item is IDownloadTaskViewModel task)
                                updater.Add(task);
                        }
                    });
                })
                .Subscribe()
                .DisposeWith(d);
            

        });
    }
}
