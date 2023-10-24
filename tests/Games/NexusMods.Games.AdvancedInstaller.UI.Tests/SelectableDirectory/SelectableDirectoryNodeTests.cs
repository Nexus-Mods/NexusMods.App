using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;
using NexusMods.Paths.TestingHelpers;
using static NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers.SelectableDirectoryVMTestHelpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.SelectableDirectory;

public class SelectableDirectoryNodeTests
{
    [Theory, AutoFileSystem]
    public void CreateTree(AbsolutePath entryDir)
    {
        // Arrange
        const string rootName = "Skyrim Special Edition";
        var fs = CreateInMemoryFs(entryDir);

        // Act
        var node = TreeEntryViewModel.Create(fs.FromUnsanitizedFullPath(entryDir.GetFullPath()),
            new GamePath(LocationId.Game, ""), rootName);

        // Assert
        node.DisplayName.Should().Be(rootName);
        AssertChildNode(node, "Meshes");
        var textures = AssertChildNode(node, "Textures");
        AssertChildNode(textures, "Armors");
    }
}
