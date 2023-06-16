using System.Reactive.Disposables;
using Avalonia;
using Avalonia.ReactiveUI;
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
        });
    }
}

