using FluentAssertions;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Games.AdvancedInstaller.Tests;

public partial class DeploymentDataTests
{
    [Fact]
    public void AddFolderMapping_Should_MapAllChildrenCorrectly_WhenMappingToSubfolderInGamePath()
    {
        // Arrange
        var data = new DeploymentData();

        // Create a File Tree
        var folderNode = TreeCreator.Create(CreateExtensionTestFileTree());

        // Act
        data.AddFolderMapping(folderNode, MakeGamePath("Data"));

        // Assert
        AssertMapping(data, "folder/file1.txt", MakeGamePath("Data/folder/file1.txt"));
        AssertMapping(data, "folder/file2.txt", MakeGamePath("Data/folder/file2.txt"));
        AssertMapping(data, "folder/subfolder/file3.txt", MakeGamePath("Data/folder/subfolder/file3.txt"));
    }

    [Fact]
    public void RemoveFolderMapping_Should_UnmapAllChildrenCorrectly()
    {
        // Arrange
        var data = new DeploymentData();

        // Create a File Tree
        var folderNode = TreeCreator.Create(CreateExtensionTestFileTree()).FindByPathFromChild("folder")!;

        // Act
        data.AddFolderMapping(folderNode, MakeGamePath("Data"));

        // Assert
        AssertMapping(data, "folder/file1.txt", MakeGamePath("Data/file1.txt"));
        AssertMapping(data, "folder/file2.txt", MakeGamePath("Data/file2.txt"));
        AssertMapping(data, "folder/subfolder/file3.txt", MakeGamePath("Data/subfolder/file3.txt"));

        // Now remove it
        data.RemoveFolderMapping(folderNode);
        data.ArchiveToOutputMap.Should().BeEmpty();
        data.OutputToArchiveMap.Should().BeEmpty();
    }

    private static GamePath MakeGamePath(string path) => new(LocationId.Game, path);
    
    public record DownloadContentEntryMock : TreeCreator.ITreeCreatorNode
    {
        public Hash Hash { get; init; }
        public Size Size { get; init; }
        public RelativePath Path { get; init; }
    }

    private static DownloadContentEntryMock[] CreateExtensionTestFileTree()
    {
        return
        [
            new () { Hash = Hash.From(1), Size = Size.From(1), Path = "folder/file1.txt" },
            new () { Hash = Hash.From(2), Size = Size.From(2), Path = "folder/file2.txt" },
            new () { Hash = Hash.From(3), Size = Size.From(3), Path = "folder/subfolder/file3.txt" },
        ];
    }

    private void AssertMapping(DeploymentData data, string archivePath, GamePath expectedOutputPath)
    {
        var map = data.ArchiveToOutputMap;
        map.Should().ContainKey(archivePath);
        map[archivePath].Should().Be(expectedOutputPath);
    }
}
