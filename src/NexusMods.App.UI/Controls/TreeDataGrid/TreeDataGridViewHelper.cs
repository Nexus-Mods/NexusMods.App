using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Settings;
using NexusMods.UI.Sdk;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Controls;

public static class TreeDataGridViewHelper
{
    /// <summary>
    /// Sets up the <see cref="TreeDataGridAdapter{TModel, TKey}"/> for the current view.
    /// </summary>
    public static void SetupTreeDataGridAdapter<TView, TViewModel, TItemModel, TKey>(
        TView view,
        Avalonia.Controls.TreeDataGrid treeDataGrid,
        Func<TViewModel, TreeDataGridAdapter<TItemModel, TKey>> getAdapter,
        bool enableDragAndDrop = false,
        bool persistState = false)
        where TView : ReactiveUserControl<TViewModel>
        where TViewModel : class, IViewModelInterface
        where TItemModel : class, ITreeDataGridItemModel<TItemModel, TKey>
        where TKey : notnull
    {
        treeDataGrid.ElementFactory = new CustomElementFactory();
        
        if (enableDragAndDrop) treeDataGrid.AutoDragDropRows = true;

        view.WhenActivated(disposables =>
        {
            // Activation and deactivation of models
            var activate = Observable.FromEventHandler<TreeDataGridRowEventArgs>(
                addHandler: handler => treeDataGrid.RowPrepared += handler,
                removeHandler: handler => treeDataGrid.RowPrepared -= handler
            ).Select(static tuple => (tuple.e.Row.Model, true));

            var deactivate = Observable.FromEventHandler<TreeDataGridRowEventArgs>(
                addHandler: handler => treeDataGrid.RowClearing += handler,
                removeHandler: handler => treeDataGrid.RowClearing -= handler
            ).Select(static tuple => (tuple.e.Row.Model, false));

            deactivate.Merge(activate)
                .Where(static tuple => tuple.Model is TItemModel)
                .Select(static tuple => ((tuple.Model as TItemModel)!, tuple.Item2))
                .Subscribe((view, getAdapter), static (tuple, state) => state.getAdapter(state.view.ViewModel!).ModelActivationSubject.OnNext(tuple))
                .AddTo(disposables);
            
            // Drag & Drop support
            if (enableDragAndDrop)
            {
                Observable.FromEventHandler<TreeDataGridRowDragStartedEventArgs>(
                        addHandler: handler => treeDataGrid.RowDragStarted += handler,
                        removeHandler: handler => treeDataGrid.RowDragStarted -= handler)
                    .Subscribe((view, getAdapter), static (eventArgs, state) =>
                            state.getAdapter(state.view.ViewModel!).OnRowDragStarted(eventArgs.sender, eventArgs.e))
                    .AddTo(disposables);
                
                Observable.FromEventHandler<TreeDataGridRowDragEventArgs>(
                        addHandler: handler => treeDataGrid.RowDragOver += handler,
                        removeHandler: handler => treeDataGrid.RowDragOver -= handler)
                    .Subscribe((view, getAdapter), static (eventArgs, state) =>
                        state.getAdapter(state.view.ViewModel!).OnRowDragOver(eventArgs.sender, eventArgs.e))
                    .AddTo(disposables);

                Observable.FromEventHandler<TreeDataGridRowDragEventArgs>(
                        addHandler: handler => treeDataGrid.RowDrop += handler,
                        removeHandler: handler => treeDataGrid.RowDrop -= handler)
                    .Subscribe((view, getAdapter), static (eventArgs, state) =>
                            state.getAdapter(state.view.ViewModel!).OnRowDrop(eventArgs.sender, eventArgs.e))
                    .AddTo(disposables);
            }
            
            // CTRL + A support
            Observable.FromEventHandler<KeyEventArgs>(
                addHandler: handler => treeDataGrid.KeyDown += handler,
                removeHandler: handler => treeDataGrid.KeyDown -= handler)
                .Where(e => 
                    e.e.Key == Key.A 
                    && e.e.KeyModifiers.HasFlag(KeyModifiers.Control)
                    )
                .Subscribe((view, getAdapter), static (eventArgs, state) =>
                {
                    var adapter = state.getAdapter(state.view.ViewModel!);
                    // Select all items
                    adapter.SelectAll();
                    
                    eventArgs.e.Handled = true;
                })
                .AddTo(disposables);
            
            // Persist treeDataGrid state on deactivation
            Disposable.Create((view, treeDataGrid, getAdapter, persistState),
                static input =>
                {
                    if (!input.persistState) return;
                    
                    var adapter = input.getAdapter(input.view.ViewModel!);
                    var sortingState = GetSortingState(input.treeDataGrid);
                    if (sortingState is not null)
                    {
                        adapter.PersistSortingState(sortingState);
                    }
                });
        });
    }
    
    
    private static TreeDataGridSortingStateSettings? GetSortingState(Avalonia.Controls.TreeDataGrid treeDataGrid)
    {
        var sortedColumn = treeDataGrid.Columns?.FirstOrDefault(col => col.SortDirection != null);
        if (sortedColumn?.Tag is not string tag)
        {
            return null;
        }
                            
        var state = new TreeDataGridSortingStateSettings
        {
            SortedColumnKey = tag,
            SortDirection = sortedColumn.SortDirection,
        };
        return state;
    }
}
