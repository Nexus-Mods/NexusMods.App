using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public sealed class SelectLocationTreeViewModel : SelectLocationTreeBaseViewModel
{
    private readonly SelectableTreeEntryViewModel _selectableTree;

    public SelectLocationTreeViewModel(AbsolutePath absPath, LocationId treeRoot, string? rootName,
        IAdvancedInstallerCoordinator coordinator)
    {
        _selectableTree = SelectableTreeEntryViewModel.Create(absPath, new GamePath(treeRoot, ""), coordinator, rootName ?? "");
        Coordinator = coordinator;
    }

    protected override ISelectableTreeEntryViewModel GetTreeData() => _selectableTree;
}
