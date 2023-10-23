using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;

internal class PreviewTreeEntryDesignViewModel : PreviewTreeEntryViewModel
{
    // public TreeEntryDesignViewModel() : base(ModContentNode<int>.FromFileTree(
    //     new FileTreeNode<RelativePath, int>(new RelativePath("BWS.bsa").GetRootComponent, new RelativePath("Textures"),
    //         false, 1))) { }

    public PreviewTreeEntryDesignViewModel() : base(new SelectableDirectoryNode
        { Status = SelectableDirectoryNodeStatus.Editing }) { }
}
