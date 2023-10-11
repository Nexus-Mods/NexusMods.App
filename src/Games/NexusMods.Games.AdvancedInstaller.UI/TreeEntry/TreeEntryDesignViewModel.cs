using NexusMods.App.UI;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using OneOf;

namespace NexusMods.Games.AdvancedInstaller.UI;

internal class TreeEntryDesignViewModel : TreeEntryViewModel
{

    public TreeEntryDesignViewModel() : base(ModContentNode<int>.FromFileTree(
        new FileTreeNode<RelativePath, int>(new RelativePath("BWS.bsa").GetRootComponent, new RelativePath("Textures"),
            false, 1))) { }
}
