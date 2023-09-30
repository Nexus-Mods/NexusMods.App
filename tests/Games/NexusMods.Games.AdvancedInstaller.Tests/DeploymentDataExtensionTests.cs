using FluentAssertions;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.Tests;

public partial class DeploymentDataTests
{
    [Fact]
    public void AddFolderMapping_Should_MapAllChildrenCorrectly_WhenMappingToSubfolderInGamePath()
    {
        // Arrange
        var data = new DeploymentData();

        // Create a File Tree
        var fileEntries = CreateExtensionTestFileTree();
        var folderNode = FileTreeNode<RelativePath, int>.CreateTree(fileEntries);

        // Act
        data.AddFolderMapping(folderNode, MakeGamePath("Data"));

        // Assert
        AssertMapping(data, "folder/file1.txt", MakeGamePath("Data/folder/file1.txt"));
        AssertMapping(data, "folder/file2.txt", MakeGamePath("Data/folder/file2.txt"));
        AssertMapping(data, "folder/subfolder/file3.txt", MakeGamePath("Data/folder/subfolder/file3.txt"));
    }

    [Fact]
    public void AddFolderMapping_Should_MapAllChildrenCorrectly_WhenMappingASubfolderInArchive()
    {
        // Arrange
        var data = new DeploymentData();

        // Create a File Tree
        var fileEntries = CreateExtensionTestFileTree();
        var folderNode = FileTreeNode<RelativePath, int>.CreateTree(fileEntries).FindNode("folder")!;

        // Act
        data.AddFolderMapping(folderNode, MakeGamePath("Data"));

        // Assert
        AssertMapping(data, "folder/file1.txt", MakeGamePath("Data/file1.txt"));
        AssertMapping(data, "folder/file2.txt", MakeGamePath("Data/file2.txt"));
        AssertMapping(data, "folder/subfolder/file3.txt", MakeGamePath("Data/subfolder/file3.txt"));
    }

    private static GamePath MakeGamePath(string path) => new(LocationId.Game, path);

    private static Dictionary<RelativePath, int> CreateExtensionTestFileTree()
    {
        var fileEntries = new Dictionary<RelativePath, int>
        {
            { new RelativePath("folder/file1.txt"), 1 },
            { new RelativePath("folder/file2.txt"), 2 },
            { new RelativePath("folder/subfolder/file3.txt"), 3 }
        };
        return fileEntries;
    }

    private void AssertMapping(DeploymentData data, string archivePath, GamePath expectedOutputPath)
    {
        var map = data.ArchiveToOutputMap;
        map.Should().ContainKey(archivePath);
        map[archivePath].Should().Be(expectedOutputPath);
    }
}
