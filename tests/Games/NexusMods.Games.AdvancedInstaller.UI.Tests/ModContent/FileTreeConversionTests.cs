using NexusMods.Games.AdvancedInstaller.UI.Resources;
using NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.ModContent;

/// <summary>
/// Tests converting <see cref="FileTreeNode{TPath,TValue}"/> into ViewModel specific nodes.
/// </summary>
public class FileTreeConversionTests : AModContentNodeTest
{
    [Fact]
    public void CanCreateNodes_Basic()
    {
        var node = CreateTestTreeNode();

        // The root node
        AssertNode(node, Language.FileTree_ALL_MOD_FILES, true, true, 5);

        // Root Directory
        AssertFileInTree(node, "BWS.bsa", false, false, 0);
        AssertFileInTree(node, "BWS - Textures.bsa", false, false, 0);
        AssertFileInTree(node, "Readme-BWS.txt", false, false, 0);

        // "Textures" Directory
        var texturesDir = node.Children.FirstOrDefault(x => x.Node.AsT0.FileName == "Textures")!.Node.AsT0;
        AssertFileInTree(texturesDir, "greenBlade.dds", false, false, 0);
        AssertFileInTree(texturesDir, "greenBlade_n.dds", false, false, 0);
        AssertFileInTree(texturesDir, "greenHilt.dds", false, false, 0);
        AssertFileInTree(texturesDir, "Armors", false, true, 3);

        // "Armors" sub-directory inside "Textures"
        var armorsDir = texturesDir.Children.FirstOrDefault(x => x.Node.AsT0.FileName == "Armors")!.Node.AsT0;
        AssertFileInTree(armorsDir, "greenArmor.dds", false, false, 0);
        AssertFileInTree(armorsDir, "greenBlade.dds", false, false, 0);
        AssertFileInTree(armorsDir, "greenHilt.dds", false, false, 0);

        // "Meshes" Directory
        AssertFileInTree(node, "Meshes", false, true, 1);
        var meshesDir = node.Children.FirstOrDefault(x => x.Node.AsT0.FileName == "Meshes")!.Node.AsT0;
        AssertFileInTree(meshesDir, "greenBlade.nif", false, false, 0);
    }
}
