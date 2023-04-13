using FluentAssertions;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Tests.New.RelativePathTests;

/// <summary>
/// Tests for utility path methods.
/// </summary>
public class UtilityTests
{
    [Fact]
    public void CanGetDepth()
    {
        var path = @"\foo\bar\baz".ToRelativePath();
        Assert.Equal(3, path.Depth);
    }

    [Fact]
    public void CanGetParent()
    {
        var path = @"\foo\bar\baz".ToRelativePath();
        @"\foo\bar".ToRelativePath().Should().Be(path.Parent);
    }

    [Fact]
    public void CanGetFileName()
    {
        @"\foo\bar".ToRelativePath().FileName.Should().BeEquivalentTo("bar".ToRelativePath());
    }

    [Fact]
    public void CanCheckStartOfPath()
    {
        var pathA = @"foo\bar\baz".ToRelativePath();
        Assert.True(pathA.StartsWith("fo"));
        Assert.True(pathA.StartsWith("Fo"));
        Assert.False(pathA.StartsWith("fooo"));
    }

    [Fact]
    public void CanCheckInFolder()
    {
        var pathA = @"foo\bar\baz.zip".ToRelativePath();
        var pathB = @"foo\bar".ToRelativePath();
        var pathC = @"fOo\Bar".ToRelativePath();
        var pathD = @"foo\barbaz".ToRelativePath();
        Assert.True(pathA.InFolder(pathB));
        Assert.True(pathA.InFolder(pathC));
        Assert.False(pathB.InFolder(pathA));
        Assert.False(pathD.InFolder(pathB));
    }

    [Theory]
    [InlineData("Desktop/Cat.png", "/home/sewer", "/home/sewer/Desktop/Cat.png")]
    [InlineData("Desktop\\Cat.png", "/home\\sewer", "/home\\sewer\\Desktop\\Cat.png")] // mixed slash
    [InlineData("/home/sewer/Desktop/Cat.png", "", "/home/sewer/Desktop/Cat.png")]
    public void RelativeTo(string expected, string parent, string child)
    {
        Assert.Equal(expected, child.ToRelativePath().RelativeTo(parent).ToString());
    }

    [Theory]
    [InlineData("home/sewer/cat.png", "/home/sewer/cat.png", 1)]
    [InlineData("sewer/cat.png", "/home/sewer/cat.png", 2)]
    [InlineData("cat.png", "/home/sewer/cat.png", 3)]
    public void DropFirst(string expected, string path, int directories)
    {
        Assert.Equal(expected, path.ToRelativePath().DropFirst(directories).ToString());
    }

    [Theory]
    [InlineData("/home/sewer/cat.png", 4)]
    public void DropFirst_TooLong(string path, int directories)
    {
        Assert.Throws<PathException>(() => path.ToRelativePath().DropFirst(directories));
    }

    [SkippableTheory]
    [InlineData(@"foo", @"foo/", false)]
    [InlineData(@"foo", @"foo/bar/baz", false)]
    [InlineData(@"foo", @"foo/bar", false)]
    [InlineData(@"foo", @"foo\", false)]
    [InlineData(@"foo", @"foo\bar\baz", false)]
    [InlineData(@"foo", @"foo\bar", false)]
    public void TopParent(string expected, string item, bool linux)
    {
        Skip.If(linux != OperatingSystem.IsLinux());

        var path = item.ToRelativePath();
        path.TopParent
            .Should()
            .Be(expected);
    }
}
