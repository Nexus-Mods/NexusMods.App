using FluentAssertions;

namespace NexusMods.Paths.Tests.New.AbsolutePathTests;

public class GetFullPathTests
{
    private readonly InMemoryFileSystem _fileSystem;

    public GetFullPathTests()
    {
        _fileSystem = new InMemoryFileSystem();
    }
    
    [SkippableTheory]
    [InlineData("/", "/", "", true)]
    [InlineData("/foo", "/", "foo", true)]
    [InlineData("/foo/bar", "/foo", "bar", true)]
    [InlineData("C:\\", "C:\\", "", false)]
    [InlineData("C:\\foo", "C:\\", "foo", false)]
    [InlineData("C:\\foo\\bar", "C:\\foo", "bar", false)]
    public void Test_GetFullPath(string expected, string directory, string fileName, bool unixLike)
        => AssertGetFullPath(expected, directory, fileName, unixLike);

    [SkippableTheory]
    [InlineData("/", "/", true)]
    [InlineData("C:\\", "C:\\", false)]
    [InlineData("/foo", "/foo", true)]
    [InlineData("C:\\foo", "C:\\foo", false)]
    public void Test_GetFullPath_WithoutFileName(string expected, string directory, bool unixLike)
        => AssertGetFullPath(expected, directory, "", unixLike);

    [SkippableTheory]
    [InlineData("/", true)]
    [InlineData("C:\\", false)]
    [InlineData("/foo", true)]
    [InlineData("C:\\foo", false)]
    public void GetFullPath_WithInsufficientBuffer(string input, bool unixLike)
    {
        Skip.If(unixLike != OSHelper.IsUnixLike());

        var path = AbsolutePath.FromFullPath(input, _fileSystem);
        var insufficientLength = path.GetFullPathLength() - 1;

        path.Invoking(x =>
            {
                Span<char> buffer = stackalloc char[insufficientLength < 0 ? 0 : insufficientLength];
                x.GetFullPath(buffer);
            })
            .Should()
            .Throw<ArgumentException>();
    }

    private void AssertGetFullPath(string expected, string directory, string fileName, bool unixLike)
    {
        Skip.If(unixLike != OSHelper.IsUnixLike());
        var path = AbsolutePath.FromDirectoryAndFileName(directory, fileName, _fileSystem);

        // GetFullPath
        path.GetFullPath().Should().Be(expected);

        // GetFullPath with Span
        Span<char> buffer = stackalloc char[path.GetFullPathLength()];
        path.GetFullPath(buffer);

        buffer.ToString().Should().Be(expected);
    }
}
