namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

internal class SelectableDirectoryEntryDesignViewModel : SelectableDirectoryEntryViewModel
{
    // public TreeEntryDesignViewModel() : base(ModContentNode<int>.FromFileTree(
    //     new FileTreeNode<RelativePath, int>(new RelativePath("BWS.bsa").GetRootComponent, new RelativePath("Textures"),
    //         false, 1))) { }

    public SelectableDirectoryEntryDesignViewModel() : base(new SelectableDirectoryNode
        { Status = SelectableDirectoryNodeStatus.Editing }) { }
}
