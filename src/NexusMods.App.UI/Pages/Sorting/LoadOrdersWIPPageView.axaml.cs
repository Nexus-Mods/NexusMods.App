using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public partial class LoadOrdersWIPPageView : ReactiveUserControl<ILoadOrdersWIPPageViewModel>
{
    public LoadOrdersWIPPageView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel,
                        vm => vm.SortingSelectionViewModel,
                        v => v.SortingSelectionView.ViewModel
                    )
                    .DisposeWith(d);
            }
        );
    }
}
