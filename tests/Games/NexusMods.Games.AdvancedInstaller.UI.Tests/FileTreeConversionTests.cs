using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.Resources;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests;

/// <summary>
/// Tests converting <see cref="FileTreeNode{TPath,TValue}"/> into ViewModel specific nodes.
/// </summary>
public class FileTreeConversionTests
{
    [Fact]
    public void CanCreateNodes_Basic()
    {
        var files = CreateTestTree();
        var tree = TreeDataGridSourceFileNode<RelativePath, int>.FromFileTree(files);

        // The root node
        AssertNode(tree, Language.FileTree_ALL_MOD_FILES, true, true, 5);

        // Root Directory
        AssertFileInTree(tree, "BWS.bsa", false, false, 0);
        AssertFileInTree(tree, "BWS - Textures.bsa", false, false, 0);
        AssertFileInTree(tree, "Readme-BWS.txt", false, false, 0);

        // "Textures" Directory
        var texturesDir = tree.Children.FirstOrDefault(x => x.FileName == "Textures")!;
        AssertFileInTree(texturesDir, "greenBlade.dds", false, false, 0);
        AssertFileInTree(texturesDir, "greenBlade_n.dds", false, false, 0);
        AssertFileInTree(texturesDir, "greenHilt.dds", false, false, 0);
        AssertFileInTree(texturesDir, "Armors", false, true, 3);

        // "Armors" sub-directory inside "Textures"
        var armorsDir = texturesDir.Children.FirstOrDefault(x => x.FileName == "Armors")!;
        AssertFileInTree(armorsDir, "greenArmor.dds", false, false, 0);
        AssertFileInTree(armorsDir, "greenBlade.dds", false, false, 0);
        AssertFileInTree(armorsDir, "greenHilt.dds", false, false, 0);

        // "Meshes" Directory
        AssertFileInTree(tree, "Meshes", false, true, 1);
        var meshesDir = tree.Children.FirstOrDefault(x => x.FileName == "Meshes")!;
        AssertFileInTree(meshesDir, "greenBlade.nif", false, false, 0);
    }

    private void AssertFileInTree(ITreeDataGridSourceFileNode root, string expectedName, bool isRoot, bool isDirectory, int expectedChildrenCount)
        => AssertNode(root.Children.FirstOrDefault(x => x.FileName == expectedName)!, expectedName, isRoot, isDirectory, expectedChildrenCount);

    private void AssertNode(ITreeDataGridSourceFileNode node, string expectedName, bool isRoot, bool isDirectory, int expectedChildrenCount)
    {
        node.FileName.Should().Be(expectedName);
        node.IsRoot.Should().Be(isRoot);
        node.IsDirectory.Should().Be(isDirectory);
        node.Children.Length.Should().Be(expectedChildrenCount);
    }

    private static FileTreeNode<RelativePath, int> CreateTestTree()
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
