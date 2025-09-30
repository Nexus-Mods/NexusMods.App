using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public partial class SortingSelectionView : ReactiveUserControl<ISortingSelectionViewModel>
{
    public SortingSelectionView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel,
                        vm => vm.ViewModels,
                        v => v.SortOrdersTabControl.ItemsSource
                    )
                    .DisposeWith(disposables);
            }
        );
    }
}

