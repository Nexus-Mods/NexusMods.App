using System.Reactive.Disposables;
using Avalonia;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Overlays.Download.Cancel;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;
using NexusMods.App.UI.Windows;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.Downloads;

public partial class InProgressView : ReactiveUserControl<IInProgressViewModel>
{
    public InProgressView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            CancelButton.Command = ViewModel!.ShowCancelDialog;

            // List of elements that are tinted blue when a download is active.
            var tintedElements = new StyledElement[]
            {
                BoldMinutesRemainingTextBlock,
                MinutesRemainingTextBlock
            };

            this.WhenAnyValue(view => view.ViewModel!.Tasks)
                .BindToUi(this, view => view.ModsDataGrid.ItemsSource)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Columns)
                .GenerateColumns(ModsDataGrid)
                .DisposeWith(d);

            // Dynamically Update Accented Items During Active Download
            this.WhenAnyValue(view => view.ViewModel!.IsRunning)
                .OnUI()
                .BindToClasses(BoldMinutesRemainingTextBlock, "UsesAccentLighterColor")
                .DisposeWith(d);
                
            this.WhenAnyValue(view => view.ViewModel!.IsRunning)
                .OnUI()
                .BindToClasses(MinutesRemainingTextBlock, "UsesAccentLighterColor")
                .DisposeWith(d);

            // Dynamically Update Title
            this.WhenAnyValue(view => view.ViewModel!.Tasks)
                .OnUI()
                .Subscribe(models =>
                {
                    InProgressTitleTextBlock.Text = $"In progress ({models.Count})";
                })
                .DisposeWith(d);

            // Dynamically Update Downloaded Bytes Text
            this.WhenAnyValue(view => view.ViewModel!.DownloadedSizeBytes, view => view.ViewModel!.TotalSizeBytes)
                .OnUI()
                .Subscribe(_ =>
                {
                    var vm = ViewModel!;
                    SizeCompletionTextBlock.Text = StringFormatters.ToGB(vm.DownloadedSizeBytes, vm.TotalSizeBytes);
                    DownloadProgressBar.Value = vm.DownloadedSizeBytes / (double)vm.TotalSizeBytes;
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
        });
    }
}

