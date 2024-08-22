using Avalonia.Controls;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.Controls;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutPage;

[UsedImplicitly]
public partial class LoadoutView : ReactiveUserControl<ILoadoutViewModel>
{
    public LoadoutView()
    {
        InitializeComponent();

        TreeDataGrid.ElementFactory = new CustomElementFactory();

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.SwitchViewCommand, view => view.SwitchView)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.ViewFilesCommand, view => view.ViewFilesButton)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.RemoveItemCommand, view => view.DeleteButton)
                .AddTo(disposables);

            var activate = Observable.FromEventHandler<TreeDataGridRowEventArgs>(
                addHandler: handler => TreeDataGrid.RowPrepared += handler,
                removeHandler: handler => TreeDataGrid.RowPrepared -= handler
            ).Select(static tuple => (tuple.e.Row.Model, true));

            var deactivate = Observable.FromEventHandler<TreeDataGridRowEventArgs>(
                addHandler: handler => TreeDataGrid.RowClearing += handler,
                removeHandler: handler => TreeDataGrid.RowClearing -= handler
            ).Select(static tuple => (tuple.e.Row.Model, false));

            deactivate.Merge(activate)
                .Where(static tuple => tuple.Model is LoadoutItemModel)
                .Select(static tuple => ((tuple.Model as LoadoutItemModel)!, tuple.Item2))
                .Subscribe(this, static (tuple, view) => view.ViewModel!.Adapter.ModelActivationSubject.OnNext(tuple))
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Adapter.Source.Value, view => view.TreeDataGrid.Source)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Adapter.IsSourceEmpty.Value, view => view.EmptyState.IsActive)
                .AddTo(disposables);
        });
    }
}

