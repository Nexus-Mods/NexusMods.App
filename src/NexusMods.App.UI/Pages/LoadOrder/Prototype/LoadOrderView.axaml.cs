using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadOrder.Prototype;

public partial class LoadOrderView : ReactiveUserControl<ILoadOrderViewModel>
{
    public LoadOrderView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
            {
                
                this.OneWayBind(ViewModel, vm => vm.SortableItems, v => v.ItemsList.ItemsSource)
                    .DisposeWith(disposables);
            }
        );
    }
}
