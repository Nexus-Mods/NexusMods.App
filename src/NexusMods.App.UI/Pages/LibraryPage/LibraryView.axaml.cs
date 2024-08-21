using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.Controls;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LibraryPage;

[UsedImplicitly]
public partial class LibraryView : ReactiveUserControl<ILibraryViewModel>
{
    public LibraryView()
    {
        InitializeComponent();

        TreeDataGrid.ElementFactory = new CustomElementFactory();

        this.WhenActivated(disposables =>
        {
            TreeDataGrid.Width = Double.NaN;
            
            this.BindCommand(ViewModel, vm => vm.SwitchViewCommand, view => view.SwitchView)
                .DisposeWith(disposables);

            var activate = Observable.FromEventHandler<TreeDataGridRowEventArgs>(
                addHandler: handler => TreeDataGrid.RowPrepared += handler,
                removeHandler: handler => TreeDataGrid.RowPrepared -= handler
            ).Select(static tuple => (tuple.e.Row.Model, true));

            var deactivate = Observable.FromEventHandler<TreeDataGridRowEventArgs>(
                addHandler: handler => TreeDataGrid.RowClearing += handler,
                removeHandler: handler => TreeDataGrid.RowClearing -= handler
            ).Select(static tuple => (tuple.e.Row.Model, false));

            deactivate.Merge(activate)
                .Where(static tuple => tuple.Model is LibraryItemModel)
                .Select(static tuple => ((tuple.Model as LibraryItemModel)!, tuple.Item2))
                .Subscribe(this, static (tuple, view) => view.ViewModel!.ActivationSubject.OnNext(tuple))
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Source, view => view.TreeDataGrid.Source)
                .DisposeWith(disposables);
            
            // TODO:
            // Bind view.EmptyState.IsActive to number of items > 0 in the source
            // Bind view.EmptyLibrarySubtitleTextBlock.Text to string based on game name
            // Bind view.EmptyLibraryLinkButton to open Nexus game page
            // Bind ToolBars buttons to 
            
            // This is a workaround to make the TreeDataGrid fill the available space
        });
    }
}
