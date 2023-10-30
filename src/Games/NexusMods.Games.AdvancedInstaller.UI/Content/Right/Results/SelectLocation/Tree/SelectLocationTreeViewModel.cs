using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

public sealed class SelectLocationTreeViewModel : SelectLocationTreeBaseViewModel
{
    private readonly TreeEntryViewModel _tree;

    public SelectLocationTreeViewModel(AbsolutePath absPath, LocationId treeRoot, string? rootName,
        IAdvancedInstallerCoordinator coordinator)
    {
        _tree = TreeEntryViewModel.Create(absPath, new GamePath(treeRoot, ""), coordinator, rootName ?? "");
        Coordinator = coordinator;
    }

    protected override ITreeEntryViewModel GetTreeData() => _tree;
}
