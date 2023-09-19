using FluentAssertions;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.DataModel.Tests;

public class GamePathFileTreeNodeTests
{

    [Theory]
    [InlineData(0, "file1.txt", true, 1)]
    [InlineData(0,"file2.txt", false, 2)]
    [InlineData(0,"foo/file2.txt", true, 2)]
    [InlineData(0,"foo/file3.txt", true, 3)]
    [InlineData(0,"foo/bar/file4.txt", true, 4)]
    [InlineData(0,"baz/bazer/file5.txt", true, 5)]
    [InlineData(1,"baz/bazer/file5.txt", false, 5)]
    public void Test_FindNode(GameFolderType folderType, string path, bool found, int value)
    {
        var tree = MakeTestTree();

        var node = tree.FindNode(new GamePath(folderType, (RelativePath)path));
        if (found)
        {
            node.Should().NotBeNull();
            node!.Path.Should().Be(new GamePath(folderType, (RelativePath)path));
            node!.Value.Should().Be(value);
        }
        else
        {
            node.Should().BeNull();
        }
    }

    private static FileTreeNode<GamePath, int> MakeTestTree()
    {
        Dictionary<GamePath, int> fileEntries;

        fileEntries = new Dictionary<GamePath, int>
        {
            { new GamePath(GameFolderType.Game,"file1.txt"), 1 },
            { new GamePath(GameFolderType.Game,"foo/file2.txt"), 2 },
            { new GamePath(GameFolderType.Game,"foo/file3.txt"), 3 },
            { new GamePath(GameFolderType.Game,"foo/bar/file4.txt"), 4 },
            { new GamePath(GameFolderType.Game,"baz/bazer/file5.txt"), 5 },
        };

        return FileTreeNode<GamePath, int>.CreateTree(fileEntries);
    }
}
