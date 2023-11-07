using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;
using NexusMods.Paths;
using static NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers.ResultsVMTestHelpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.Results;

public class NodeCreationTests
{
    // Note: Tests here assume all items should be given 'new', as is currently intended to be at time of writing.

    [Fact]
    public void AddSingleNode_CreatesChildren()
    {
        // Create the tree from single entry.
        var root = TreeEntryViewModel.Create(new GamePath(LocationId.Game, "Textures/Armors/greenHilt.dds"), false);
        root.FileName.Should().Be("Game");
        root.IsRoot.Should().BeTrue();
        root.IsDirectory.Should().BeTrue();

        var textures = root.GetNode("Textures");
        textures.Should().NotBeNull();
        textures!.IsNew.Should().BeTrue();

        var armors = textures.GetNode("Armors");
        armors.Should().NotBeNull();
        armors!.IsNew.Should().BeTrue();

        var greenHilt = armors.GetNode("greenHilt.dds");
        greenHilt.Should().NotBeNull();
        greenHilt!.IsNew.Should().BeTrue();
    }

    [Fact]
    public void AddChild_AddsAllChildren()
    {
        // Create empty tree, then add all paths separately.
        var root = TreeEntryViewModel.Create(new GamePath(LocationId.Game, ""), true);
        foreach (var file in GetPaths())
            root.AddChildren(file, false);

        // Now verify the structure.
        AssertChildNode(root, "BWS.bsa");
        AssertChildNode(root, "BWS - Textures.bsa");
        AssertChildNode(root, "Readme-BWS.txt");

        var textures = AssertChildNode(root, "Textures");
        AssertChildNode(textures, "greenBlade.dds");
        AssertChildNode(textures, "greenBlade_n.dds");
        AssertChildNode(textures, "greenHilt.dds");

        var armors = AssertChildNode(textures, "Armors");
        AssertChildNode(armors, "greenArmor.dds");
        AssertChildNode(armors, "greenBlade.dds");
        AssertChildNode(armors, "greenHilt.dds");

        var meshes = AssertChildNode(root, "Meshes");
        AssertChildNode(meshes, "greenBlade.nif");
    }
}
