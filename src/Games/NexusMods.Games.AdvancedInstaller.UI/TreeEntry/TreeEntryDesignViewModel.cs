using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

namespace NexusMods.Games.AdvancedInstaller.UI;

internal class TreeEntryDesignViewModel : TreeEntryViewModel
{

    // public TreeEntryDesignViewModel() : base(ModContentNode<int>.FromFileTree(
    //     new FileTreeNode<RelativePath, int>(new RelativePath("BWS.bsa").GetRootComponent, new RelativePath("Textures"),
    //         false, 1))) { }

    public TreeEntryDesignViewModel() : base(new SelectableDirectoryNode{ Status = SelectableDirectoryNodeStatus.Editing}) { }
}
