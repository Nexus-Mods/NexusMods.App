using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI;

internal class TreeEntryDesignViewModel : TreeEntryViewModel
{

    public TreeEntryDesignViewModel() : base(ModContentNode<int>.FromFileTree(
        new FileTreeNode<RelativePath, int>(new RelativePath("BWS.bsa").GetRootComponent, new RelativePath("Textures"),
            false, 1))) { }
}
