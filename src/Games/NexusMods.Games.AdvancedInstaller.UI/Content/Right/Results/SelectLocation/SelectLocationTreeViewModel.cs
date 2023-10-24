using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

public sealed class SelectLocationTreeViewModel : SelectLocationTreeDesignViewModel
{
    private readonly TreeEntryViewModel _tree;

    public SelectLocationTreeViewModel(AbsolutePath absPath, LocationId treeRoot, string? rootName)
    {
        _tree = TreeEntryViewModel.Create(absPath, new GamePath(treeRoot, ""), rootName ?? "");
    }

    protected override ITreeEntryViewModel GetTreeData() => _tree;
}
