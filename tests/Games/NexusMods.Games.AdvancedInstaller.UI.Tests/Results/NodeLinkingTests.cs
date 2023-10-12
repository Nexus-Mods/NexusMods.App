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
        data.OutputToArchiveMap[new GamePath(LocationId.Game, "Textures/Armors/greenArmor.dds")].Should().Be("Textures/Armors/greenArmor.dds");
        data.OutputToArchiveMap[new GamePath(LocationId.Game, "Textures/Armors/greenBlade.dds")].Should().Be("Textures/Armors/greenBlade.dds");
        data.OutputToArchiveMap[new GamePath(LocationId.Game, "Textures/Armors/greenHilt.dds")].Should().Be("Textures/Armors/greenHilt.dds");
    }

    private (ModContentNode<int> node, DeploymentData data, PreviewEntryNode target)
        CommonSetup()
    {
        var node = ModContentNodeTestHelpers.CreateTestTreeNode();
        var data = new DeploymentData();

        var target = PreviewEntryNode.Create(new GamePath(LocationId.Game, ""), true);
        foreach (var file in GetPaths())
            target.AddChild(file, false);

        return (node, data, target);
    }
}
