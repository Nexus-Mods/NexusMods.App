using FluentAssertions;

namespace NexusMods.Paths.Tests;

public class RelativePathTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("foo", "")]
    [InlineData("foo.txt", ".txt")]
    public void Test_Extension(string input, string expectedExtension)
    {
        var path = new RelativePath(input);
        path.Extension.Should().Be(new Extension(expectedExtension));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("foo", "foo")]
    [InlineData("foo/bar/baz", "baz")]
    public void Test_FileName(string input, string expectedFileName)
    {
        var path = new RelativePath(input);
        path.FileName.Should().Be(expectedFileName);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("foo", 0)]
    [InlineData("foo/bar", 1)]
    [InlineData("foo/bar/baz", 2)]
    public void Test_Depth(string input, int expectedDepth)
    {
        var path = new RelativePath(input);
        path.Depth.Should().Be(expectedDepth);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("foo", "")]
    [InlineData("foo/bar", "foo")]
    [InlineData("foo/bar/baz", "foo/bar")]
    public void Test_Parent(string input, string expectedParent)
    {
        var path = new RelativePath(input);
        path.Parent.Should().Be(expectedParent);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("foo", "foo")]
    [InlineData("foo/bar", "foo")]
    [InlineData("foo/bar/baz", "foo")]
    public void Test_TopParent(string input, string expectedParent)
    {
        var path = new RelativePath(input);
        path.TopParent.Should().Be(expectedParent);
    }

    [Theory]
    [InlineData("foo", ".txt", "foo.txt")]
    [InlineData("foo.txt", ".md", "foo.md")]
    public void Test_ReplaceExtension(string input, string extension, string expectedOutput)
    {
        var path = new RelativePath(input);
        var actualOutput = path.ReplaceExtension(new Extension(extension));
        actualOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData("foo", ".txt", "foo.txt")]
    [InlineData("foo.txt", ".md", "foo.txt.md")]
    public void Test_WithExtension(string input, string extension, string expectedOutput)
    {
        var path = new RelativePath(input);
        var actualOutput = path.WithExtension(new Extension(extension));
        actualOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData("foo", "bar", "foo/bar")]
    [InlineData("foo/bar", "baz", "foo/bar/baz")]
    [InlineData("foo", "bar/baz", "foo/bar/baz")]
    public void Test_Join(string left, string right, string expectedOutput)
    {
        var leftPath = new RelativePath(left);
        var rightPath = new RelativePath(right);
        var actualOutput = leftPath.Join(rightPath);
        actualOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData("foo", "bar", false)]
    [InlineData("foo", "foo", true)]
    public void Test_StartsWith(string left, string right, bool expected)
    {
        var path = new RelativePath(left);
        var actual = path.StartsWith(right);
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("foo", "bar", false)]
    [InlineData("foo/bar", "foo", true)]
    [InlineData("foo/bar/baz", "foo/bar", true)]
    public void Test_InFolder(string left, string right, bool expected)
    {
        var leftPath = new RelativePath(left);
        var rightPath = new RelativePath(right);
        var actual = leftPath.InFolder(rightPath);
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar/baz", 0, "foo/bar/baz")]
    [InlineData("foo/bar/baz", 1, "bar/baz")]
    [InlineData("foo/bar/baz", 2, "baz")]
    [InlineData("foo/bar/baz", 3, "")]
    public void Test_DropFirst(string input, int count, string expectedOutput)
    {
        var path = new RelativePath(input);
        var output = path.DropFirst(count);
        output.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData("foo/bar/baz", "foo", "bar/baz")]
    [InlineData("foo/bar/baz", "foo/bar", "baz")]
    public void Test_RelativeTo(string left, string right, string expectedOutput)
    {
        var leftPath = new RelativePath(left);
        var rightPath = new RelativePath(right);
        var actualOutput = leftPath.RelativeTo(rightPath);
        actualOutput.Should().Be(expectedOutput);
    }
}
