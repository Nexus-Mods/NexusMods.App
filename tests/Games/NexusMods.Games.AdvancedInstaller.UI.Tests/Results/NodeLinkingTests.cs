using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Games.AdvancedInstaller.UI.Preview;
using NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;
using NexusMods.Paths;
using static NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers.ResultsVMTestHelpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.Results;

public class NodeLinkingTests
{
    [Fact]
    public void CanLinkFolders()
    {
        // Arrange & Act
        var (node, data, target) = CommonSetup();
        var armorsDir = node.GetNode("Textures").GetNode("Armors");
        (armorsDir as ModContentTreeEntryViewModel<int>)?.BeginSelect();

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

    // Confirms files can be re-linked from another folder, while state is kept consistent.
    [Fact]
    public void CanReLinkFolders()
    {
        // Arrange & Act
        var (node, data, target) = CommonSetup();
        var armorsDir = node.GetNode("Textures").GetNode("Armors");
        (armorsDir as ModContentTreeEntryViewModel<int>)?.BeginSelect();
        var linkTarget = target.GetChild("Textures/Armors")!;

        // Link Armors Directory
        armorsDir.Link(data, linkTarget, false);

        // Assert the correct folder as added in output end
        data.OutputToArchiveMap[new GamePath(LocationId.Game, "Textures/Armors/greenArmor.dds")].Should()
            .Be("Textures/Armors/greenArmor.dds");
        data.OutputToArchiveMap[new GamePath(LocationId.Game, "Textures/Armors/greenBlade.dds")].Should()
            .Be("Textures/Armors/greenBlade.dds");
        data.OutputToArchiveMap[new GamePath(LocationId.Game, "Textures/Armors/greenHilt.dds")].Should()
            .Be("Textures/Armors/greenHilt.dds");

        // Re-link now
        var armorsDir2 = node.GetNode("Textures").GetNode("Armors2");
        (armorsDir2 as ModContentTreeEntryViewModel<int>)?.BeginSelect();
        armorsDir2.Link(data, linkTarget, false);
        data.OutputToArchiveMap[new GamePath(LocationId.Game, "Textures/Armors/greenArmor.dds")].Should()
            .Be("Textures/Armors2/greenArmor.dds");
        data.OutputToArchiveMap[new GamePath(LocationId.Game, "Textures/Armors/greenBlade.dds")].Should()
            .Be("Textures/Armors2/greenBlade.dds");
        data.OutputToArchiveMap[new GamePath(LocationId.Game, "Textures/Armors/greenHilt.dds")].Should()
            .Be("Textures/Armors2/greenHilt.dds");
    }

    [Fact]
    public void CanUnlinkFoldersRecursively()
    {
        // Arrange & Act
        var (node, data, target) = CommonSetup();
        var armorsDir = node.GetNode("Textures").GetNode("Armors");

        // Link Armors Directory, then unlink root.
        armorsDir.Link(data, target.GetChild("Textures/Armors")!, false);
        target.Unlink(false);

        // Note: There is no direct link in 'target', the children are linked.
        // Assert everything got deleted
        data.OutputToArchiveMap.Should().BeEmpty();
        data.ArchiveToOutputMap.Should().BeEmpty();

        // Root node children should be deleted.
        target.Children.Should().BeEmpty();
    }

    [Fact]
    public void CanUnlinkFolders_FromModContent()
    {
        // Arrange & Act
        var (node, data, target) = CommonSetup();
        var armorsDir = node.GetNode("Textures").GetNode("Armors");
        (armorsDir as ModContentTreeEntryViewModel<int>)?.BeginSelect();
        var targetDir = target.GetChild("Textures/Armors")!;

        // Link Armors Directory, then unlink root.
        armorsDir.Link(data, targetDir, false);
        armorsDir.Unlink(false);

        // Note: There is no direct link in 'target', the children are linked.
        // Assert everything got deleted
        data.OutputToArchiveMap.Should().BeEmpty();
        data.ArchiveToOutputMap.Should().BeEmpty();

        // Root node children should be deleted.
        targetDir.Children.Should().BeEmpty();
        armorsDir.LinkedItem.Should().BeNull();
        targetDir.LinkedItem.Should().BeNull();

        // Assert empty folder was deleted.
        target.GetChild("Textures/Armors").Should().BeNull();
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
        (texturesTarget as PreviewTreeEntryViewModel)!.Unlink(false);

        // Note: There is no direct link in 'target', the children are linked.
        // Assert everything got deleted
        data.OutputToArchiveMap.Should().BeEmpty();
        data.ArchiveToOutputMap.Should().BeEmpty();

        // Armors children should be deleted.
        target.Children.Should().NotContain(x => x.FileName == "Textures");
    }

    private (ModContentTreeEntryViewModel<int> node, DeploymentData data, PreviewTreeEntryViewModel target)
        CommonSetup()
    {
        var node = ModContentVMTestHelpers.CreateTestTreeNode();
        var data = node.Coordinator.Data;

        var target = PreviewTreeEntryViewModel.Create(new GamePath(LocationId.Game, ""), true);
        foreach (var file in GetPaths())
            target.AddChildren(file, false);

        return (node, data, target);
    }
}
