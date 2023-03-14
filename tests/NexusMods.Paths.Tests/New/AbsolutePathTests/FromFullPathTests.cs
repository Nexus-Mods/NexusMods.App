using FluentAssertions;

namespace NexusMods.Paths.Tests.New.AbsolutePathTests;

public class FromFullPathTests
{
    [SkippableTheory]
    [InlineData("/", "/", "/", "", true)]
    [InlineData("/foo", "/foo", "/", "foo", true)]
    [InlineData("/foo/bar", "/foo/bar", "/foo", "bar", true)]
    [InlineData("/foo/bar/", "/foo/bar", "/foo", "bar", true)]
    [InlineData("foo", "foo", null, "foo", true)]
    [InlineData("C:\\", "C:\\", "C:\\", "", false)]
    [InlineData("C:\\foo", "C:\\foo", "C:\\", "foo", false)]
    [InlineData("C:\\foo\\bar", "C:\\foo\\bar", "C:\\foo", "bar", false)]
    [InlineData("C:\\foo\\bar\\", "C:\\foo\\bar", "C:\\foo", "bar", false)]
    [InlineData("foo", "foo", null, "foo", false)]
    public void Test_FromFullPath(string input, string expectedFullPath,
        string? expectedDirectory, string expectedFileName, bool linux)
    {
        Skip.IfNot(OperatingSystem.IsLinux() && linux);
        var path = AbsolutePath.FromFullPath(input);
        path.GetFullPath().Should().Be(expectedFullPath);
        path.Directory.Should().Be(expectedDirectory);
        path.FileName.Should().Be(expectedFileName);
    }
}
