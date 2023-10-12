using NexusMods.Games.AdvancedInstaller.UI.Resources;
using NexusMods.Paths.FileTree;
using static NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers.ModContentNodeTestHelpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.ModContent;

/// <summary>
/// Tests converting <see cref="FileTreeNode{TPath,TValue}"/> into ViewModel specific nodes.
/// </summary>
public class FileTreeConversionTests
{
    [Fact]
    public void CanCreateNodes_Basic()
    {
        var node = CreateTestTreeNode();

        // The root node
        AssertNode(node, Language.FileTree_ALL_MOD_FILES, true, true, 5);

        // Root Directory
        AssertChildNode(node, "BWS.bsa", false, false, 0);
        AssertChildNode(node, "BWS - Textures.bsa", false, false, 0);
        AssertChildNode(node, "Readme-BWS.txt", false, false, 0);

        // "Textures" Directory
        var texturesDir = node.Children.FirstOrDefault(x => x.Node.AsT0.FileName == "Textures")!.Node.AsT0;
        AssertChildNode(texturesDir, "greenBlade.dds", false, false, 0);
        AssertChildNode(texturesDir, "greenBlade_n.dds", false, false, 0);
        AssertChildNode(texturesDir, "greenHilt.dds", false, false, 0);
        AssertChildNode(texturesDir, "Armors", false, true, 3);

        // "Armors" sub-directory inside "Textures"
        var armorsDir = texturesDir.Children.FirstOrDefault(x => x.Node.AsT0.FileName == "Armors")!.Node.AsT0;
        AssertChildNode(armorsDir, "greenArmor.dds", false, false, 0);
        AssertChildNode(armorsDir, "greenBlade.dds", false, false, 0);
        AssertChildNode(armorsDir, "greenHilt.dds", false, false, 0);

        // "Meshes" Directory
        AssertChildNode(node, "Meshes", false, true, 1);
        var meshesDir = node.Children.FirstOrDefault(x => x.Node.AsT0.FileName == "Meshes")!.Node.AsT0;
        AssertChildNode(meshesDir, "greenBlade.nif", false, false, 0);
    }
}
