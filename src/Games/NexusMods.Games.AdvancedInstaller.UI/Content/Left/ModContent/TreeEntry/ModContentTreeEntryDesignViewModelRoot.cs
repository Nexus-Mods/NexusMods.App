using System.Diagnostics.CodeAnalysis;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

/// <summary>
///     Design ViewModel for root node.
/// </summary>
[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
internal class ModContentTreeEntryDesignViewModelRoot : ModContentTreeEntryViewModel<int>
{
    public ModContentTreeEntryDesignViewModelRoot()
    {
        Node = new FileTreeNode<RelativePath, int>(new RelativePath("BWS.bsa").GetRootComponent,
            new RelativePath("Textures"),
            false, 1);

        Parent = null!;
        Children = GC.AllocateUninitializedArray<IModContentTreeEntryViewModel>(0);
        IsTopLevel = false;
        SetStatus(ModContentNodeStatus.Default);
    }
}
