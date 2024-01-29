using FluentAssertions;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.Tests;

public partial class DeploymentDataTests
{
    [Fact]
    public void EmitOperations_ShouldEmitCorrectAModFileInstructions()
    {
        // Arrange
        var data = new DeploymentData();

        var folderNode = ModFileTree.Create(CreateEmitTestFileTree());
        data.AddFolderMapping(folderNode, MakeGamePath(""));

        // Act
        var emittedOperations = data.EmitOperations(folderNode).ToList();

        // Assert
        emittedOperations.Should().NotBeEmpty();
        var first = emittedOperations[0] as StoredFile;
        first!.To.Should().Be(MakeGamePath("folder/file1.txt"));
        first.Hash.Should().Be(Hash.From(1));
        first.Size.Should().Be(Size.From(1));

        var second = emittedOperations[1] as StoredFile;
        second!.To.Should().Be(MakeGamePath("folder/file2.txt"));
        second.Hash.Should().Be(Hash.From(2));
        second.Size.Should().Be(Size.From(2));

        var third = emittedOperations[2] as StoredFile;
        third!.To.Should().Be(MakeGamePath("folder/subfolder/file3.txt"));
        third.Hash.Should().Be(Hash.From(3));
        third.Size.Should().Be(Size.From(3));
    }

    private static ModFileTreeSource[] CreateEmitTestFileTree()
    {
        return
        [
            new(1, 1, "folder/file1.txt"),
            new(2, 2, "folder/file2.txt"),
            new(3, 3, "folder/subfolder/file3.txt")
        ];
    }
}
