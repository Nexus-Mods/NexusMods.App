using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;
using NexusMods.Games.AdvancedInstaller.UI.Resources;
using NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;
using NexusMods.Paths;
using NSubstitute;
using NSubstitute.ReceivedExtensions;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.ModContent;

/// <summary>
///     Tests related to the linking of mod content nodes to output directories.
/// </summary>
public class NodeLinkingTests : AModContentNodeTest
{
    [Fact]
    public void CanLinkFolders()
    {
        // Arrange
        var node = CreateTestTreeNode();
        var data = new DeploymentData();
        var armorsDir = node.GetNode("Textures").GetNode("Armors");

        var target = Substitute.For<IModContentBindingTarget>();
        var path = new GamePath(LocationId.Game, "");
        target.Bind(Arg.Any<IUnlinkableItem>()).Returns(path);

        // Act
        // Bind Leaf Directory
        armorsDir.Link(data, target);

        // Assert leaf is bound.
        target.Received().Bind(Arg.Any<IUnlinkableItem>());
        data.ArchiveToOutputMap.Count.Should().Be(3);
        data.OutputToArchiveMap.Count.Should().Be(3);
        data.ArchiveToOutputMap["Textures/Armors/greenArmor.dds"].Should().Be(new GamePath(LocationId.Game, "greenArmor.dds"));
        data.ArchiveToOutputMap["Textures/Armors/greenBlade.dds"].Should().Be(new GamePath(LocationId.Game, "greenBlade.dds"));
        data.ArchiveToOutputMap["Textures/Armors/greenHilt.dds"].Should().Be(new GamePath(LocationId.Game, "greenHilt.dds"));
    }
}
