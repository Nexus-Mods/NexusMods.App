using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

public partial class SelectLocationTreeView : ReactiveUserControl<ISelectLocationTreeViewModel>
{
    public SelectLocationTreeView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            this.OneWayBind(ViewModel, vm => vm.Tree, view => view.SelectTreeDataGrid.Source).DisposeWith(d)
        );
    }
}
