using NexusMods.Backend.FileExtractor;
using TUnit.Assertions;


namespace NexusMods.Backend.Tests.FileExtractor;

public class PathsHelperTests
{
    [Test]
    [Arguments("foo/bar", "foo/bar")]
    [Arguments("foo/bar/", "foo/bar")]
    [Arguments("foo\\bar\\", "foo\\bar")]
    [Arguments("foo /bar ", "foo/bar")]
    [Arguments("foo./bar.", "foo/bar")]
    [Arguments("foo.\\bar.", "foo\\bar")]
    public async Task Test_FixPath(string input, string expected)
    {
        var actual = PathsHelper.FixPath(input);
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    [Arguments("foo/bar.baz", "foo/bar.baz")]
    [Arguments("foo bar.baz", "foo bar.baz")]
    [Arguments("foo.bar.", "foo.bar")]
    [Arguments("foo/bar ", "foo/bar")]
    [Arguments("foo/bar .", "foo/bar")]
    [Arguments("foo/bar. ", "foo/bar")]
    public async Task Test_FixFileName(string input, string expected)
    {
        var actual = PathsHelper.FixFileName(input);
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    [Arguments("foo/bar /", "foo/bar")]
    [Arguments("foo/bar. /", "foo/bar")]
    public async Task Test_FixDirectoryName(string input, string expected)
    {
        var actual = PathsHelper.FixDirectoryName(input);
        await Assert.That(actual).IsEqualTo(expected);
    }
}
