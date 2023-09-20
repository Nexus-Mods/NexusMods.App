using FluentAssertions;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.Tests;

public partial class DeploymentDataTests
{
    [Fact]
    public void EmitOperations_ShouldEmitCorrectAModFileInstructions()
    {
        // Arrange
        var data = new DeploymentData();

        var fileEntries = CreateEmitTestFileTree();
        var folderNode = FileTreeNode<RelativePath, ModSourceFileEntry>.CreateTree(fileEntries);
        data.AddFolderMapping(folderNode, MakeGamePath(""));

        // Act
        var emittedOperations = data.EmitOperations(folderNode).ToList();

        // Assert
        emittedOperations.Should().NotBeEmpty();
        var first = emittedOperations[0] as FromArchive;
        first!.To.Should().Be(MakeGamePath("folder/file1.txt"));
        first.Hash.Should().Be(Hash.From(1));
        first.Size.Should().Be(Size.From(1));

        var second = emittedOperations[1] as FromArchive;
        second!.To.Should().Be(MakeGamePath("folder/file2.txt"));
        second.Hash.Should().Be(Hash.From(2));
        second.Size.Should().Be(Size.From(2));

        var third = emittedOperations[2] as FromArchive;
        third!.To.Should().Be(MakeGamePath("folder/subfolder/file3.txt"));
        third.Hash.Should().Be(Hash.From(3));
        third.Size.Should().Be(Size.From(3));
    }

    private static Dictionary<RelativePath, ModSourceFileEntry> CreateEmitTestFileTree()
    {
        return new Dictionary<RelativePath, ModSourceFileEntry>
        {
            {
                new RelativePath("folder/file1.txt"), new ModSourceFileEntry
                {
                    Hash = Hash.From(1),
                    Size = Size.From(1),
                    StreamFactory = null!
                }
            },
            {
                new RelativePath("folder/file2.txt"), new ModSourceFileEntry
                {
                    Hash = Hash.From(2),
                    Size = Size.From(2),
                    StreamFactory = null!
                }
            },
            {
                new RelativePath("folder/subfolder/file3.txt"), new ModSourceFileEntry
                {
                    Hash = Hash.From(3),
                    Size = Size.From(3),
                    StreamFactory = null!
                }
            }
        };
    }
}
