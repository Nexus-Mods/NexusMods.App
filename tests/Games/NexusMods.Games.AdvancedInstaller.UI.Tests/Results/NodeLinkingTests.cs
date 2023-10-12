using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;
using NexusMods.Paths;
using static NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers.ResultsNodeTestHelpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.Results;

public class NodeLinkingTests
{
    [Fact]
    public void CanLinkFolders()
    {
        // Arrange & Act
        var (node, data, target) = CommonSetup();
        var armorsDir = node.GetNode("Textures").GetNode("Armors");

        // Link Armors Directory
        armorsDir.Link(data, target.GetChild("Textures/Armors")!, false);

        // Assert the correct folder as added in output end
        data.OutputToArchiveMap[new GamePath(LocationId.Game, "Textures/Armors/greenArmor.dds")].Should()
            .Be("Textures/Armors/greenArmor.dds");
        data.OutputToArchiveMap[new GamePath(LocationId.Game, "Textures/Armors/greenBlade.dds")].Should()
            .Be("Textures/Armors/greenBlade.dds");
        data.OutputToArchiveMap[new GamePath(LocationId.Game, "Textures/Armors/greenHilt.dds")].Should()
            .Be("Textures/Armors/greenHilt.dds");
    }

    [Fact]
    public void CanUnlinkFoldersRecursively()
    {
        // Arrange & Act
        var (node, data, target) = CommonSetup();
        var armorsDir = node.GetNode("Textures").GetNode("Armors");

        // Link Armors Directory, then unlink root.
        armorsDir.Link(data, target.GetChild("Textures/Armors")!, false);
        target.Unlink(data);

        // Note: There is no direct link in 'target', the children are linked.
        // Assert everything got deleted
        data.OutputToArchiveMap.Should().BeEmpty();
        data.ArchiveToOutputMap.Should().BeEmpty();

        // Root node children should be deleted.
        target.Children.Should().BeEmpty();
    }

    [Fact]
    public void CanUnlinkFolders_WithNonRootNode()
    {
        // Arrange & Act
        var (node, data, target) = CommonSetup();
        var armorsDir = node.GetNode("Textures").GetNode("Armors");

        // Link Armors Directory, then unlink root.
        var texturesTarget = target.GetChild("Textures")!;
        armorsDir.Link(data, texturesTarget, false);
        (texturesTarget as PreviewEntryNode)!.Unlink(data);

        // Note: There is no direct link in 'target', the children are linked.
        // Assert everything got deleted
        data.OutputToArchiveMap.Should().BeEmpty();
        data.ArchiveToOutputMap.Should().BeEmpty();

        // Armors children should be deleted.
        target.Children.Should().NotContain(x => x.Node.AsT2.FileName == "Textures");
    }

    private (ModContentNode<int> node, DeploymentData data, PreviewEntryNode target)
        CommonSetup()
    {
        var node = ModContentNodeTestHelpers.CreateTestTreeNode();
        var data = new DeploymentData();

        var target = PreviewEntryNode.Create(new GamePath(LocationId.Game, ""), true);
        foreach (var file in GetPaths())
            target.AddChildren(file, false);

        return (node, data, target);
    }
}
