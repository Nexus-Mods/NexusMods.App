using FluentAssertions;

namespace NexusMods.FileExtractor.Tests;

public class PathsHelperTests
{
    [Theory]
    [InlineData("foo/bar", "foo/bar")]
    [InlineData("foo/bar/", "foo/bar")]
    [InlineData("foo\\bar\\", "foo\\bar")]
    [InlineData("foo /bar ", "foo/bar")]
    [InlineData("foo./bar.", "foo/bar")]
    [InlineData("foo.\\bar.", "foo\\bar")]
    public void Test_FixPath(string input, string expected)
    {
        var actual = PathsHelper.FixPath(input);
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar.baz", "foo/bar.baz")]
    [InlineData("foo bar.baz", "foo bar.baz")]
    [InlineData("foo.bar.", "foo.bar")]
    [InlineData("foo/bar ", "foo/bar")]
    [InlineData("foo/bar .", "foo/bar")]
    [InlineData("foo/bar. ", "foo/bar")]
    public void Test_FixFileName(string input, string expected)
    {
        var actual = PathsHelper.FixFileName(input);
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar /", "foo/bar")]
    [InlineData("foo/bar. /", "foo/bar")]
    public void Test_FixDirectoryName(string input, string expected)
    {
        var actual = PathsHelper.FixDirectoryName(input);
        actual.Should().Be(expected);
    }
}
