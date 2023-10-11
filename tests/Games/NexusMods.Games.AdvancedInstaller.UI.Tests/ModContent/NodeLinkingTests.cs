using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;
using NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;
using NexusMods.Paths;
using NSubstitute;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.ModContent;

/// <summary>
///     Tests related to the linking of mod content nodes to output directories.
/// </summary>
public class NodeLinkingTests : AModContentNodeTest
{
    [Fact]
    public void CanLinkFolders()
    {
        // Arrange & Act
        var (node, data, target) = CommonSetup();
        var armorsDir = node.GetNode("Textures").GetNode("Armors");
        armorsDir.Link(data, target);

        // Assert
        target.Received().Bind(Arg.Any<IUnlinkableItem>());
        AssertArmorsLinked(data);
        armorsDir.GetNode("greenArmor.dds").Status.Should().Be(ModContentNodeStatus.IncludedViaParent);
        armorsDir.GetNode("greenBlade.dds").Status.Should().Be(ModContentNodeStatus.IncludedViaParent);
        armorsDir.GetNode("greenHilt.dds").Status.Should().Be(ModContentNodeStatus.IncludedViaParent);
    }

    [Fact]
    public void CanLinkFiles()
    {
        // Arrange & Act
        var (node, data, target) = CommonSetup();
        var greenArmor = node.GetNode("Textures").GetNode("Armors").GetNode("greenArmor.dds");
        greenArmor.Link(data, target);

        // Assert
        target.Received().Bind(Arg.Any<IUnlinkableItem>());
        greenArmor.Status.Should().Be(ModContentNodeStatus.IncludedExplicit);
        data.ArchiveToOutputMap.Count.Should().Be(1);
        data.ArchiveToOutputMap["Textures/Armors/greenArmor.dds"].Should()
            .Be(new GamePath(LocationId.Game, "greenArmor.dds"));
    }

    [Fact]
    public void CanUnlinkFolders()
    {
        // Arrange & Act
        var (node, data, target) = CommonSetup();
        var armorsDir = node.GetNode("Textures").GetNode("Armors");
        armorsDir.Link(data, target);

        // Unlink assert that everything is empty.
        armorsDir.Unlink(data);
        data.ArchiveToOutputMap.Should().BeEmpty();
        data.OutputToArchiveMap.Should().BeEmpty();
        armorsDir.Status.Should().Be(ModContentNodeStatus.Default);
        armorsDir.GetNode("greenArmor.dds").Status.Should().Be(ModContentNodeStatus.Default);
        armorsDir.GetNode("greenBlade.dds").Status.Should().Be(ModContentNodeStatus.Default);
        armorsDir.GetNode("greenHilt.dds").Status.Should().Be(ModContentNodeStatus.Default);
    }

    [Fact]
    public void CanUnlinkFiles()
    {
        // Arrange & Act
        var (node, data, target) = CommonSetup();
        var greenArmor = node.GetNode("Textures").GetNode("Armors").GetNode("greenArmor.dds");
        greenArmor.Link(data, target);

        // Assert
        greenArmor.Unlink(data);
        data.ArchiveToOutputMap.Count.Should().Be(0);
        greenArmor.Status.Should().Be(ModContentNodeStatus.Default);
    }

    private (ModContentNode<int> node, DeploymentData data, IModContentBindingTarget target) CommonSetup()
    {
        var node = CreateTestTreeNode();
        var data = new DeploymentData();
        var target = Substitute.For<IModContentBindingTarget>();

        var path = new GamePath(LocationId.Game, "");
        target.Bind(Arg.Any<IUnlinkableItem>()).Returns(path);

        return (node, data, target);
    }

    private void AssertArmorsLinked(DeploymentData data)
    {
        data.ArchiveToOutputMap.Count.Should().Be(3);
        data.OutputToArchiveMap.Count.Should().Be(3);
        data.ArchiveToOutputMap["Textures/Armors/greenArmor.dds"].Should()
            .Be(new GamePath(LocationId.Game, "greenArmor.dds"));
        data.ArchiveToOutputMap["Textures/Armors/greenBlade.dds"].Should()
            .Be(new GamePath(LocationId.Game, "greenBlade.dds"));
        data.ArchiveToOutputMap["Textures/Armors/greenHilt.dds"].Should()
            .Be(new GamePath(LocationId.Game, "greenHilt.dds"));
    }
}
