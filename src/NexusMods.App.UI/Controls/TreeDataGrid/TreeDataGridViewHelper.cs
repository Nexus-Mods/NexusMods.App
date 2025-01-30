using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.UI;
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
        TreeDataGrid treeDataGrid,
        Func<TViewModel, TreeDataGridAdapter<TItemModel, TKey>> getAdapter)
        where TView : ReactiveUserControl<TViewModel>
        where TViewModel : class, IViewModelInterface
        where TItemModel : class, ITreeDataGridItemModel<TItemModel, TKey>
        where TKey : notnull
    {
        treeDataGrid.ElementFactory = new CustomElementFactory();

        view.WhenActivated(disposables =>
        {
            var activate = Observable.FromEventHandler<TreeDataGridRowEventArgs>(
                addHandler: handler => treeDataGrid.RowPrepared += handler,
                removeHandler: handler => treeDataGrid.RowPrepared -= handler
            ).Select(static tuple => (tuple.e.Row.Model, true));

            var deactivate = Observable.FromEventHandler<TreeDataGridRowEventArgs>(
                addHandler: handler => treeDataGrid.RowClearing += handler,
                removeHandler: handler => treeDataGrid.RowClearing -= handler
            ).Select(static tuple => (tuple.e.Row.Model, false));

            // NOTE(erri120): TreeDataGridRow doesn't invoke RowClearing when it gets detached
            // from the visual tree. It does invoke RowPrepared when it gets attached with previous
            // context, so this is likely an oversight. Regardless, it messes with our activation/deactivation
            // system since now you can have situations where a row will never get deactivated if the entire TreeDataGrid
            // gets detached from the visual tree.
            // Example: ContentControl or TabControl that sometimes has a TreeDataGrid and sometimes not
            var rowDetached = Observable.FromEventHandler<TreeDataGridRowEventArgs>(
                addHandler: handler => treeDataGrid.RowPrepared += handler,
                removeHandler: handler => treeDataGrid.RowPrepared -= handler
            )
                .Select(static tuple => tuple.e.Row)
                .SelectMany(static row => Observable.FromEventHandler<VisualTreeAttachmentEventArgs>(
                    addHandler: handler => row.DetachedFromVisualTree += handler,
                    removeHandler: handler => row.DetachedFromVisualTree -= handler
                ))
                .Select(static tuple => tuple.sender as TreeDataGridRow)
                .Select(static row => (row?.Model, false));

            deactivate.Merge(rowDetached).Merge(activate)
                .Where(static tuple => tuple.Model is TItemModel)
                .Select(static tuple => ((tuple.Model as TItemModel)!, tuple.Item2))
                .Subscribe((view, getAdapter), static (tuple, state) => state.getAdapter(state.view.ViewModel!).ModelActivationSubject.OnNext(tuple))
                .AddTo(disposables);
        });
    }
}
