using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

[ExcludeFromCodeCoverage]
public partial class SelectLocationTreeView : ReactiveUserControl<ISelectLocationTreeViewModel>
{
    public SelectLocationTreeView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            this.OneWayBind<ISelectLocationTreeViewModel, SelectLocationTreeView, HierarchicalTreeDataGridSource<ISelectableTreeEntryViewModel>, ITreeDataGridSource?>(ViewModel, vm => vm.Tree, view => view.SelectTreeDataGrid.Source).DisposeWith(d)
        );
    }
}
