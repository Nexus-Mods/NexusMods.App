using FluentAssertions;
using NexusMods.DataModel.Abstractions.Games;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.DataModel.Tests;

public class GamePathFileTreeNodeTests
{

    [Theory]
    [InlineData("Game", "file1.txt", true, 1)]
    [InlineData("Game","file2.txt", false, 2)]
    [InlineData("Game","foo/file2.txt", true, 2)]
    [InlineData("Game","foo/file3.txt", true, 3)]
    [InlineData("Game","foo/bar/file4.txt", true, 4)]
    [InlineData("Game","baz/bazer/file5.txt", true, 5)]
    [InlineData("Saves","baz/bazer/file5.txt", false, 5)]
    public void Test_FindNode(string locationId, string path, bool found, int value)
    {
        var tree = MakeTestTree();

        var node = tree.FindNode(new GamePath(LocationId.From(locationId), (RelativePath)path));
        if (found)
        {
            node.Should().NotBeNull();
            node!.Path.Should().Be(new GamePath(LocationId.From(locationId), (RelativePath)path));
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
            { new GamePath(LocationId.Game,"file1.txt"), 1 },
            { new GamePath(LocationId.Game,"foo/file2.txt"), 2 },
            { new GamePath(LocationId.Game,"foo/file3.txt"), 3 },
            { new GamePath(LocationId.Game,"foo/bar/file4.txt"), 4 },
            { new GamePath(LocationId.Game,"baz/bazer/file5.txt"), 5 },
        };

        return FileTreeNode<GamePath, int>.CreateTree(fileEntries);
    }
}
