using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Tests;

public class GamePathTests
{
    private readonly InMemoryFileSystem _fileSystem;

    public GamePathTests()
    {
        _fileSystem = new InMemoryFileSystem();
    }

    [Fact]
    public void CanComparePaths()
    {
        var pathA = new GamePath(GameFolderType.Game, "foo/bar.zip");
        var pathB = new GamePath(GameFolderType.Game, "Foo/bar.zip");
        var pathC = new GamePath(GameFolderType.Preferences, "foo/bar.zip");
        var pathD = new GamePath(GameFolderType.Game, "foo/bar.pex");

        Assert.Equal(pathA, pathB);
        Assert.NotEqual(pathA, pathC);
        Assert.NotEqual(pathA, pathD);

        Assert.True(pathA == pathB);
        Assert.False(pathA == pathC);
        Assert.True(pathA != pathC);
    }

    [Fact]
    public void CanTreatLikeIPath()
    {
        var pathA = new GamePath(GameFolderType.Game, "foo/bar.zip");
        var ipath = (IPath)pathA;

        Assert.Equal(KnownExtensions.Zip, ipath.Extension);
        Assert.Equal("bar.zip".ToRelativePath(), ipath.FileName);
    }

    [Fact]
    public void CanGetHashCode()
    {
        var pathA = new GamePath(GameFolderType.Game, "foo/bar.zip");
        var pathB = new GamePath(GameFolderType.Game, "foo/ba.zip");

        Assert.Equal(pathA.GetHashCode(), pathA.GetHashCode());
        Assert.NotEqual(pathA.GetHashCode(), pathB.GetHashCode());
    }

    [Fact]
    public void CanConvertToString()
    {
        var pathA = new GamePath(GameFolderType.Game, "foo/bar.zip");
        var pathB = new GamePath(GameFolderType.Saves, "foo/ba.zip");
        Assert.Equal("{Game}/foo/bar.zip", pathA.ToString());
        Assert.Equal("{Saves}/foo/ba.zip", pathB.ToString());
    }

    [Fact]
    public void CanGetPathRelativeTo()
    {
        var baseFolder = _fileSystem.GetKnownPath(KnownPath.CurrentDirectory);
        var pathA = new GamePath(GameFolderType.Game, "foo/bar");
        Assert.Equal(baseFolder.Combine("foo/bar"), pathA.Combine(baseFolder));
    }

}
