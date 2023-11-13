using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

[ExcludeFromCodeCoverage]
public partial class LocationTreeContainerView : ReactiveUserControl<ILocationTreeContainerViewModel>
{
    public LocationTreeContainerView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            this.OneWayBind<ILocationTreeContainerViewModel, LocationTreeContainerView,
                HierarchicalTreeDataGridSource<TreeNodeVM<ISelectableTreeEntryViewModel, GamePath>>, ITreeDataGridSource?>(ViewModel,
                vm => vm.Tree, view => view.SelectTreeDataGrid.Source).DisposeWith(d)
        );
    }
}
