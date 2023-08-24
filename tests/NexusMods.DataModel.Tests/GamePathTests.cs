using FluentAssertions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.DataModel.Tests;

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

    [Theory]
    [InlineData(GameFolderType.Game, "", "")]
    [InlineData(GameFolderType.Game, "foo", "foo")]
    [InlineData(GameFolderType.Game, "foo/bar", "bar")]
    public void Test_Name(GameFolderType folderType, string input, string expected)
    {
        var path = new GamePath(folderType, (RelativePath)input);
        path.Name.Should().Be(expected);
    }


    [Theory]
    [InlineData(GameFolderType.Game, "", "")]
    [InlineData(GameFolderType.Game, "foo", "")]
    [InlineData(GameFolderType.Game, "foo/bar", "foo")]
    [InlineData(GameFolderType.Game, "foo/bar/baz", "foo/bar")]
    public void Test_Parent(GameFolderType folderType, string input, string expectedParent)
    {
        var path = new GamePath(folderType, (RelativePath)input);
        path.Parent.Should().Be(new GamePath(folderType, (RelativePath)expectedParent));
    }

    [Theory]
    [InlineData(GameFolderType.Game, "", "")]
    [InlineData(GameFolderType.Game, "foo", "")]
    [InlineData(GameFolderType.AppData, "foo/bar", "")]
    [InlineData(GameFolderType.Saves, "foo/bar/baz", "")]
    public void Test_GetRootComponent(GameFolderType folderType, string input, string expectedRootComponent)
    {
        var path = new GamePath(folderType, (RelativePath)input);
        path.GetRootComponent.Should().Be(new GamePath(folderType, (RelativePath)expectedRootComponent));
    }

    [Theory]
    [InlineData(GameFolderType.Game, "", new string[] { })]
    [InlineData(GameFolderType.Game, "foo", new string[] { "foo" })]
    [InlineData(GameFolderType.Game, "foo/bar", new string[] { "foo", "bar" })]
    [InlineData(GameFolderType.Saves, "foo/bar/baz", new string[] { "foo", "bar", "baz" })]
    public void Test_Parts(GameFolderType folderType, string input, string[] expectedParts)
    {
        var path = new GamePath(folderType, (RelativePath)input);
        path.Parts.Should().BeEquivalentTo(expectedParts.Select(x => new RelativePath(x)));
    }

    [Theory]
    [InlineData(GameFolderType.Game, "", new string[] { ""})]
    [InlineData(GameFolderType.Game, "foo", new string[] { "foo", "" })]
    [InlineData(GameFolderType.Saves, "foo/bar", new string[] { "foo/bar", "foo", "" })]
    [InlineData(GameFolderType.Game, "foo/bar/baz", new string[] { "foo/bar/baz", "foo/bar", "foo", "" })]
    public void Test_GetAllParents(GameFolderType folderType, string input, string[] expectedParts)
    {
        var path = new GamePath(folderType, (RelativePath)input);
        path.GetAllParents().Should()
            .BeEquivalentTo(expectedParts.Select(x => new GamePath(folderType, (RelativePath)x)));
    }

    [Theory]
    [InlineData(GameFolderType.Game, "", "")]
    [InlineData(GameFolderType.Game, "foo", "foo")]
    [InlineData(GameFolderType.AppData, "foo/bar", "foo/bar")]
    [InlineData(GameFolderType.Saves, "foo/bar/baz", "foo/bar/baz")]
    public void Test_GetNonRootPart(GameFolderType folderType, string input, string expected)
    {
        var path = new GamePath(folderType, (RelativePath)input);
        path.GetNonRootPart().Should().Be((RelativePath)expected);
    }

    [Theory]
    [InlineData(GameFolderType.Game, "", true)]
    [InlineData(GameFolderType.Saves, "foo", true)]
    [InlineData(GameFolderType.AppData, "foo/bar", true)]
    public void Test_IsRooted(GameFolderType folderType, string input, bool expected)
    {
        var path = new GamePath(folderType, (RelativePath)input);
        path.IsRooted.Should().Be(expected);
    }

    [Theory]
    [InlineData(GameFolderType.Game, "foo", GameFolderType.Game, "bar", false)]
    [InlineData(GameFolderType.Game, "foo/bar", GameFolderType.Game, "foo", true)]
    [InlineData(GameFolderType.Game, "foo/bar/baz", GameFolderType.Game, "foo/bar", true)]
    [InlineData(GameFolderType.Game, "foo", GameFolderType.Saves, "bar", false)]
    [InlineData(GameFolderType.Saves, "foo/bar", GameFolderType.Game, "foo", false)]
    [InlineData(GameFolderType.Game, "foo/bar/baz", GameFolderType.AppData, "foo/bar", false)]
    public void Test_InFolder(GameFolderType folderTypeLeft, string left, GameFolderType folderTypeRight, string right,
        bool expected)
    {
        var leftPath = new GamePath(folderTypeLeft, (RelativePath)left);
        var rightPath = new GamePath(folderTypeRight, (RelativePath)right);
        var actual = leftPath.InFolder(rightPath);
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(GameFolderType.Game,"",GameFolderType.Game, "", true)]
    [InlineData(GameFolderType.Game,"",GameFolderType.Game, "foo", false)]
    [InlineData(GameFolderType.Game,"foo",GameFolderType.Game, "bar", false)]
    [InlineData(GameFolderType.Game,"foo",GameFolderType.Game, "", true)]
    [InlineData(GameFolderType.Game,"foo/bar/baz",GameFolderType.Game, "", true)]
    [InlineData(GameFolderType.Game,"foo/bar/baz",GameFolderType.Game, "foo", true)]
    [InlineData(GameFolderType.Game,"foo/bar/baz",GameFolderType.Game, "foo/bar", true)]
    [InlineData(GameFolderType.Game,"foobar",GameFolderType.Game, "foo", false)]
    [InlineData(GameFolderType.Game,"foo/bar/baz",GameFolderType.Game, "foo/baz", false)]
    [InlineData(GameFolderType.Game,"",GameFolderType.AppData, "", false)]
    [InlineData(GameFolderType.Game,"foo/bar/baz",GameFolderType.AppData, "", false)]
    [InlineData(GameFolderType.Game,"foo/bar/baz",GameFolderType.AppData, "foo", false)]
    [InlineData(GameFolderType.Game,"foo/bar/baz",GameFolderType.AppData, "foo/bar", false)]
    [InlineData(GameFolderType.Game,"foo",GameFolderType.AppData, "", false)]
    public void Test_StartsWithGamePath(GameFolderType folderTypeChild, string child,GameFolderType folderTypeParent, string parent, bool expected)
    {
        var childPath = new GamePath(folderTypeChild, (RelativePath)child);
        var parentPath = new GamePath(folderTypeParent, (RelativePath)parent);
        var actual = childPath.StartsWith(parentPath);
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(GameFolderType.Game,"", "", true)]
    [InlineData(GameFolderType.Game,"", "foo", false)]
    [InlineData(GameFolderType.Game,"foo", "bar", false)]
    [InlineData(GameFolderType.Game,"foo", "", true)]
    [InlineData(GameFolderType.Game,"foo/bar/baz", "", true)]
    [InlineData(GameFolderType.Game,"foo/bar/baz", "bar/baz", true)]
    [InlineData(GameFolderType.Game,"foo/bar/baz", "foo/bar/baz", true)]
    [InlineData(GameFolderType.Game,"foobar", "bar", false)]
    [InlineData(GameFolderType.Game,"foo/bar/baz", "foo/baz", false)]
    public void Test_EndsWithRelative(GameFolderType folderType,string child, string parent, bool expected)
    {
        var childPath = new GamePath(folderType, (RelativePath)child);
        var parentPath = (RelativePath)parent;
        var actual = childPath.EndsWith(parentPath);
        actual.Should().Be(expected);
    }
}
