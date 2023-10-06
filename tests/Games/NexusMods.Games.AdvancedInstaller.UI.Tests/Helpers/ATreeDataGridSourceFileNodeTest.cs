using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;

public abstract class ATreeDataGridSourceFileNodeTest
{
    protected ModContentNode<RelativePath, int>? GetChildNode(IModContentFileNode root,
        string fileName)
    {
        return root.Children.FirstOrDefault(x => x.FileName == fileName) as
            ModContentNode<RelativePath, int>;
    }

    protected void AssertFileInTree(IModContentFileNode root, string expectedName, bool isRoot,
        bool isDirectory, int expectedChildrenCount)
    {
        AssertNode(root.Children.FirstOrDefault(x => x.FileName == expectedName)!, expectedName, isRoot, isDirectory,
            expectedChildrenCount);
    }

    protected void AssertNode(IModContentFileNode node, string expectedName, bool isRoot, bool isDirectory,
        int expectedChildrenCount)
    {
        node.FileName.Should().Be(expectedName);
        node.IsRoot.Should().Be(isRoot);
        node.IsDirectory.Should().Be(isDirectory);
        node.Children.Length.Should().Be(expectedChildrenCount);
    }

    protected ModContentNode<RelativePath, int> CreateTestTreeNode()
    {
        return ModContentNode<RelativePath, int>.FromFileTree(CreateTestTree());
    }

    protected FileTreeNode<RelativePath, int> CreateTestTree()
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
}
