using FluentAssertions;

namespace NexusMods.Paths.Tests.New.AbsolutePathTests;

public class FromFullPathTests
{
    private readonly InMemoryFileSystem _fileSystem;

    public FromFullPathTests()
    {
        _fileSystem = new InMemoryFileSystem();
    }
    
    [SkippableTheory]
    [InlineData("/", "/", "/", "", true)]
    [InlineData("/foo", "/foo", "/", "foo", true)]
    [InlineData("/foo/bar", "/foo/bar", "/foo", "bar", true)]
    [InlineData("/foo/bar/", "/foo/bar", "/foo", "bar", true)]
    [InlineData("/foo/bar/baz", "/foo/bar/baz", "/foo/bar", "baz", true)]
    [InlineData("C:\\", "C:\\", "C:\\", "", false)]
    [InlineData("C:\\foo", "C:\\foo", "C:\\", "foo", false)]
    [InlineData("C:\\foo\\bar", "C:\\foo\\bar", "C:\\foo", "bar", false)]
    [InlineData("C:\\foo\\bar\\", "C:\\foo\\bar", "C:\\foo", "bar", false)]
    [InlineData("C:\\foo\\bar\\baz", "C:\\foo\\bar\\baz", "C:\\foo\\bar", "baz", false)]
    public void Test_FromFullPath(
        string input,
        string expectedFullPath,
        string expectedDirectory,
        string expectedFileName,
        bool linux)
    {
        Skip.If(linux != OperatingSystem.IsLinux());
        var path = AbsolutePath.FromFullPath(input, _fileSystem);
        path.GetFullPath().Should().Be(expectedFullPath);
        path.Directory.Should().Be(expectedDirectory);
        path.FileName.Should().Be(expectedFileName);
    }

    [Theory]
    [InlineData("foo")]
    public void Test_ShouldError_NotRootedPath(string input)
    {
        Action act = () => AbsolutePath.FromFullPath(input, _fileSystem);

        act.Should().Throw<ArgumentException>();
    }
}
