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
    [InlineData(0, "", "")]
    [InlineData(0, "foo", "foo")]
    [InlineData(0, "foo/bar", "bar")]
    public void Test_Name(GameFolderType folderType, string input, string expected)
    {
        var path = new GamePath(folderType, (RelativePath)input);
        path.Name.Should().Be(expected);
    }


    [Theory]
    [InlineData(0, "", "")]
    [InlineData(0, "foo", "")]
    [InlineData(0, "foo/bar", "foo")]
    [InlineData(0, "foo/bar/baz", "foo/bar")]
    public void Test_Parent(GameFolderType folderType, string input, string expectedParent)
    {
        var path = new GamePath(folderType, (RelativePath)input);
        path.Parent.Should().Be(new GamePath(folderType, (RelativePath)expectedParent));
    }

    [Theory]
    [InlineData(0, "", "")]
    [InlineData(0, "foo", "")]
    [InlineData(3, "foo/bar", "")]
    [InlineData(1, "foo/bar/baz", "")]
    public void Test_GetRootComponent(GameFolderType folderType, string input, string expectedRootComponent)
    {
        var path = new GamePath(folderType, (RelativePath)input);
        path.GetRootComponent.Should().Be(new GamePath(folderType, (RelativePath)expectedRootComponent));
    }

    [Theory]
    [InlineData(0, "", new string[] { })]
    [InlineData(0, "foo", new string[] { "foo" })]
    [InlineData(0, "foo/bar", new string[] { "foo", "bar" })]
    [InlineData(1, "foo/bar/baz", new string[] { "foo", "bar", "baz" })]
    public void Test_Parts(GameFolderType folderType, string input, string[] expectedParts)
    {
        var path = new GamePath(folderType, (RelativePath)input);
        path.Parts.Should().BeEquivalentTo(expectedParts.Select(x => new RelativePath(x)));
    }

    [Theory]
    [InlineData(0, "", new string[] { ""})]
    [InlineData(0, "foo", new string[] { "foo", "" })]
    [InlineData(1, "foo/bar", new string[] { "foo/bar", "foo", "" })]
    [InlineData(0, "foo/bar/baz", new string[] { "foo/bar/baz", "foo/bar", "foo", "" })]
    public void Test_GetAllParents(GameFolderType folderType, string input, string[] expectedParts)
    {
        var path = new GamePath(folderType, (RelativePath)input);
        path.GetAllParents().Should()
            .BeEquivalentTo(expectedParts.Select(x => new GamePath(folderType, (RelativePath)x)));
    }

    [Theory]
    [InlineData(0, "", "")]
    [InlineData(0, "foo", "foo")]
    [InlineData(3, "foo/bar", "foo/bar")]
    [InlineData(1, "foo/bar/baz", "foo/bar/baz")]
    public void Test_GetNonRootPart(GameFolderType folderType, string input, string expected)
    {
        var path = new GamePath(folderType, (RelativePath)input);
        path.GetNonRootPart().Should().Be((RelativePath)expected);
    }

    [Theory]
    [InlineData(0, "", true)]
    [InlineData(1, "foo", true)]
    [InlineData(3, "foo/bar", true)]
    public void Test_IsRooted(GameFolderType folderType, string input, bool expected)
    {
        var path = new GamePath(folderType, (RelativePath)input);
        path.IsRooted.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, "foo", 0, "bar", false)]
    [InlineData(0, "foo/bar", 0, "foo", true)]
    [InlineData(0, "foo/bar/baz", 0, "foo/bar", true)]
    [InlineData(0, "foo", 1, "bar", false)]
    [InlineData(1, "foo/bar", 0, "foo", false)]
    [InlineData(0, "foo/bar/baz", 3, "foo/bar", false)]
    public void Test_InFolder(GameFolderType folderTypeLeft, string left, GameFolderType folderTypeRight, string right,
        bool expected)
    {
        var leftPath = new GamePath(folderTypeLeft, (RelativePath)left);
        var rightPath = new GamePath(folderTypeRight, (RelativePath)right);
        var actual = leftPath.InFolder(rightPath);
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(0,"",0, "", true)]
    [InlineData(0,"",0, "foo", false)]
    [InlineData(0,"foo",0, "bar", false)]
    [InlineData(0,"foo",0, "", true)]
    [InlineData(0,"foo/bar/baz",0, "", true)]
    [InlineData(0,"foo/bar/baz",0, "foo", true)]
    [InlineData(0,"foo/bar/baz",0, "foo/bar", true)]
    [InlineData(0,"foobar",0, "foo", false)]
    [InlineData(0,"foo/bar/baz",0, "foo/baz", false)]
    [InlineData(0,"",3, "", false)]
    [InlineData(0,"foo/bar/baz",3, "", false)]
    [InlineData(0,"foo/bar/baz",3, "foo", false)]
    [InlineData(0,"foo/bar/baz",3, "foo/bar", false)]
    [InlineData(0,"foo",3, "", false)]
    public void Test_StartsWithGamePath(GameFolderType folderTypeChild, string child,GameFolderType folderTypeParent, string parent, bool expected)
    {
        var childPath = new GamePath(folderTypeChild, (RelativePath)child);
        var parentPath = new GamePath(folderTypeParent, (RelativePath)parent);
        var actual = childPath.StartsWith(parentPath);
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(0,"", "", true)]
    [InlineData(0,"", "foo", false)]
    [InlineData(0,"foo", "bar", false)]
    [InlineData(0,"foo", "", true)]
    [InlineData(0,"foo/bar/baz", "", true)]
    [InlineData(0,"foo/bar/baz", "bar/baz", true)]
    [InlineData(0,"foo/bar/baz", "foo/bar/baz", true)]
    [InlineData(0,"foobar", "bar", false)]
    [InlineData(0,"foo/bar/baz", "foo/baz", false)]
    public void Test_EndsWithRelative(GameFolderType folderType,string child, string parent, bool expected)
    {
        var childPath = new GamePath(folderType, (RelativePath)child);
        var parentPath = (RelativePath)parent;
        var actual = childPath.EndsWith(parentPath);
        actual.Should().Be(expected);
    }
}
