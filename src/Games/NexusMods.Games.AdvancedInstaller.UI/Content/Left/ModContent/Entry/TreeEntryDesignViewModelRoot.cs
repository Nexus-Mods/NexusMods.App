using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

/// <summary>
///     Design ViewModel for root node.
/// </summary>
internal class TreeEntryDesignViewModelRoot : TreeEntryViewModel<int>
{
    public TreeEntryDesignViewModelRoot()
    {
        Node = new FileTreeNode<RelativePath, int>(new RelativePath("BWS.bsa").GetRootComponent,
            new RelativePath("Textures"),
            false, 1);

        Parent = null!;
        Children = GC.AllocateUninitializedArray<ITreeEntryViewModel>(0);
        IsTopLevel = false;
        SetStatus(ModContentNodeStatus.Default);
    }
}
