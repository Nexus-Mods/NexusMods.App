using System.Diagnostics;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.ReactiveUI;
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
                .Subscribe(isRunning =>
                {
                    // TODO: I (Sewer) am not particularly a fan of this; but I'm not sure of the best alternative for now.
                    if (isRunning)
                    {
                        foreach (var element in tintedElements)
                            element.Classes.Add("UsesAccentLighterColor");
                    }
                    else
                    {
                        foreach (var element in tintedElements)
                            element.Classes.Remove("UsesAccentLighterColor");
                    }
                })
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

