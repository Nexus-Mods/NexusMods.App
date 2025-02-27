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

            deactivate.Merge(activate)
                .Where(static tuple => tuple.Model is TItemModel)
                .Select(static tuple => ((tuple.Model as TItemModel)!, tuple.Item2))
                .Subscribe((view, getAdapter), static (tuple, state) => state.getAdapter(state.view.ViewModel!).ModelActivationSubject.OnNext(tuple))
                .AddTo(disposables);
        });
    }
}
