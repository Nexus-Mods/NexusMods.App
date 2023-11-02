﻿using System.Diagnostics.CodeAnalysis;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
internal class ModContentDesignViewModel : ModContentBaseViewModel
{
    protected override ITreeEntryViewModel InitTreeData() => CreateTestTree();

    private static ITreeEntryViewModel CreateTestTree()
    {
        var fileEntries = new Dictionary<RelativePath, int>
        {
            { new RelativePath("BWS.bsa"), 1 },
            { new RelativePath("BWS - Textures.bsa"), 2 },
            { new RelativePath("Readme-BWS.txt"), 3 },
            { new RelativePath("Textures/greenBlade.dds"), 4 },
            { new RelativePath("Textures/greenBlade_n.dds"), 5 },
            { new RelativePath("Textures/greenHilt.dds"), 6 },
            { new RelativePath("Textures/Armors/greenArmor.dds"), 7 },
            { new RelativePath("Textures/Armors/greenBlade.dds"), 8 },
            { new RelativePath("Textures/Armors/greenHilt.dds"), 9 },
            { new RelativePath("Meshes/greenBlade.nif"), 10 }
        };

        var tree = FileTreeNode<RelativePath, int>.CreateTree(fileEntries);
        return TreeEntryViewModel<int>.FromFileTree(tree, new DummyCoordinator());
    }
}
