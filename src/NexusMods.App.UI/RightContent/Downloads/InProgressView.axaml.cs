using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DynamicData;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.Downloads;

public partial class InProgressView : ReactiveUserControl<IInProgressViewModel>
{
    public InProgressView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
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

            this.OneWayBind(ViewModel, vm => vm.Tasks, view => view.ModsDataGrid.ItemsSource)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Columns)
                .GenerateColumns(ModsDataGrid)
                .DisposeWith(d);

            // Dynamically Update Accented Items During Active Download
            this.WhenAnyValue(view => view.ViewModel!.IsRunning)
                .OnUI()
                .BindToClasses(BoldMinutesRemainingTextBlock, StyleConstants.TextBlock.UsesAccentLighterColor)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.IsRunning)
                .OnUI()
                .BindToClasses(MinutesRemainingTextBlock, StyleConstants.TextBlock.UsesAccentLighterColor)
                .DisposeWith(d);

            // Dynamically Update Title
            this.WhenAnyValue(view => view.ViewModel!.ActiveDownloadCount)
                .OnUI()
                .Subscribe(count =>
                {
                    InProgressTitleTextBlock.Text = StringFormatters.ToDownloadsInProgressTitle(count);
                })
                .DisposeWith(d);

            // Dynamically Update Downloaded Bytes Text
            this.WhenAnyValue(view => view.ViewModel!.DownloadedSizeBytes, view => view.ViewModel!.TotalSizeBytes)
                .OnUI()
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
                .OnUI()
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

            // Bind Selected Items
            Observable.FromEventPattern<SelectionChangedEventArgs>(
                    addHandler => ModsDataGrid.SelectionChanged += addHandler,
                    removeHandler => ModsDataGrid.SelectionChanged -= removeHandler)
                .Do(_ =>
                {
                    ViewModel!.SelectedTasks.Edit(updater =>
                    {
                        updater.Clear();
                        foreach (var item in ModsDataGrid.SelectedItems)
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
