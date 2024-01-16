using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using DynamicData.Binding;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Resources;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.Downloads;

public partial class InProgressView : ReactiveUserControl<IInProgressViewModel>
{
    public InProgressView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.ShowCancelDialog)
                .BindToUi(this, view => view.CancelButton.Command)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.SuspendCurrentTask)
                .BindToUi(this, view => view.PauseButton.Command)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.SuspendAllTasks)
                .BindToUi(this, view => view.PauseAllButton.Command)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Tasks)
                .BindToUi(this, view => view.ModsDataGrid.ItemsSource)
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
            this.WhenAnyValue(view => view.ViewModel!.Tasks)
                .OnUI()
                .Select(models => models.ToObservableChangeSet())
                .Subscribe(x =>
                {
                    x.Subscribe(_ =>
                    {
                        InProgressTitleTextBlock.Text = StringFormatters.ToDownloadsInProgressTitle(ViewModel!.Tasks.Count);
                    }).DisposeWith(d);
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
                    if (DownloadProgressBar.Value == 0)
                    {
                        DownloadProgressBar.IsVisible = false;
                    }
                    else
                    {
                        DownloadProgressBar.IsVisible = true;
                    }
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

            // Bind Selected Item
            this.Bind(ViewModel!, model => model.SelectedTask, view => view.ModsDataGrid.SelectedItem)
                .DisposeWith(d);
        });
    }
}
