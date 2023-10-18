using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;
using static NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers.SelectableDirectoryNodeTestHelpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.SelectableDirectory;

public class SelectableDirectoryNodeTests
{
    [Fact]
    public void CreateTree()
    {
        // Arrange
        var fs = CreateInMemoryFs();

        // Act
        var node = SelectableDirectoryNode.Create(fs.FromUnsanitizedFullPath(""), new GamePath(LocationId.Game, ""));

        // Assert
        AssertChildNode(node, "Meshes");
        var textures = AssertChildNode(node, "Textures");
        AssertChildNode(textures, "Armors");
    }
}
