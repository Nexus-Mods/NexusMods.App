using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using DynamicData.Binding;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;
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
                    SizeCompletionTextBlock.Text = StringFormatters.ToGB(vm.DownloadedSizeBytes, vm.TotalSizeBytes);
                    DownloadProgressBar.Value = vm.DownloadedSizeBytes / Math.Max(1.0, vm.TotalSizeBytes);
                })
                .DisposeWith(d);

            // Dynamically Update Time Remaining Text
            this.WhenAnyValue(view => view.ViewModel!.SecondsRemaining)
                .OnUI()
                .Subscribe(_ =>
                {
                    var vm = ViewModel!;
                    BoldMinutesRemainingTextBlock.Text = StringFormatters.ToTimeRemainingShort(vm.SecondsRemaining);
                })
                .DisposeWith(d);

            // Bind Selected Item
            this.Bind(ViewModel!, model => model.SelectedTask, view => view.ModsDataGrid.SelectedItem)
                .DisposeWith(d);
        });
    }
}

