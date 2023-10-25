using FluentAssertions;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;

internal static class ModContentVMTestHelpers
{
    internal static TreeEntryViewModel<int>? GetChildNode(ITreeEntryViewModel root,
        string fileName)
    {
        return root.Children.FirstOrDefault(x => x.FileName == fileName)! as
            TreeEntryViewModel<int>;
    }

    internal static void AssertChildNode(ITreeEntryViewModel root, string expectedName, bool isRoot,
        bool isDirectory, int expectedChildrenCount)
    {
        AssertNode(root.GetNode(expectedName), expectedName, isRoot, isDirectory,
            expectedChildrenCount);
    }

    internal static ITreeEntryViewModel GetNode(ITreeEntryViewModel root, string expectedName) =>
        root.Children.FirstOrDefault(x => x.FileName == expectedName)!;

    internal static void AssertNode(ITreeEntryViewModel node, string expectedName, bool isRoot, bool isDirectory,
        int expectedChildrenCount)
    {
        node.FileName.Should().Be(expectedName);
        node.IsRoot.Should().Be(isRoot);
        node.IsDirectory.Should().Be(isDirectory);
        node.Children.Length.Should().Be(expectedChildrenCount);
    }

    internal static TreeEntryViewModel<int> CreateTestTreeNode()
    {
        return TreeEntryViewModel<int>.FromFileTree(CreateTestTree());
    }

    internal static FileTreeNode<RelativePath, int> CreateTestTree()
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

        return FileTreeNode<RelativePath, int>.CreateTree(fileEntries);
    }

    internal static FileTreeNode<RelativePath, ModSourceFileEntry> CreateTestTreeMSFE()
    {
        var fileEntries = new Dictionary<RelativePath, ModSourceFileEntry>
        {
            { new RelativePath("BWS.bsa"), null! },
            { new RelativePath("BWS - Textures.bsa"), null! },
            { new RelativePath("Readme-BWS.txt"), null! },
            { new RelativePath("Textures/greenBlade.dds"), null! },
            { new RelativePath("Textures/greenBlade_n.dds"), null! },
            { new RelativePath("Textures/greenHilt.dds"), null! },
            { new RelativePath("Textures/Armors/greenArmor.dds"), null! },
            { new RelativePath("Textures/Armors/greenBlade.dds"), null! },
            { new RelativePath("Textures/Armors/greenHilt.dds"), null! },
            { new RelativePath("Meshes/greenBlade.nif"), null! }
        };

        return FileTreeNode<RelativePath, ModSourceFileEntry>.CreateTree(fileEntries);
    }
}

internal static class ModContentNodeExtensions
{
    public static ITreeEntryViewModel GetNode(this ITreeEntryViewModel root, string expectedName) =>
        root.Children.FirstOrDefault(x => x.FileName == expectedName)!;
}
