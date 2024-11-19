using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting.Prototype;

public partial class LoadOrderView : ReactiveUserControl<ILoadOrderViewModel>
{
    public LoadOrderView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
            {
                
                this.OneWayBind(ViewModel, vm => vm.SortableItems, v => v.ItemsList.ItemsSource)
                    .DisposeWith(disposables);
                
                this.OneWayBind(ViewModel, vm => vm.TreeSource, v => v.SortOrderTreeDataGrid.Source)
                    .DisposeWith(disposables);
            }
        );
    }
    
    private void OnRowDrop(object? sender, TreeDataGridRowDragEventArgs e)
    {
        // NOTE(Al12rs): This is important in case the source is read-only, otherwise TreeDataGrid will attempt to
        // move the items, updating the source collection, throwing an exception in the process.
        e.Handled = true;
    }
}
