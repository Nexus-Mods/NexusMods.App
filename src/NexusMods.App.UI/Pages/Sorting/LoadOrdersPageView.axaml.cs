using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public partial class LoadOrdersPageView : ReactiveUserControl<ILoadOrdersPageViewModel>
{
    public LoadOrdersPageView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel, vm => vm.LoadOrderViewModels, v => v.SortOrdersTabControl.ItemsSource)
                    .DisposeWith(disposables);
            }
        );
    }
}

