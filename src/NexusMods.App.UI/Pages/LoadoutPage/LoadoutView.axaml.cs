using System.Reactive.Disposables;
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
                .Subscribe(this, static (tuple, view) => view.ViewModel!.ActivationSubject.OnNext(tuple))
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Source, view => view.TreeDataGrid.Source)
                .DisposeWith(disposables);
        });
    }
}

