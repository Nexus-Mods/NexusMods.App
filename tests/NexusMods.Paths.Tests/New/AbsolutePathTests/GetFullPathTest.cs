using FluentAssertions;

namespace NexusMods.Paths.Tests.New.AbsolutePathTests;

public class GetFullPathTests
{
    [SkippableTheory]
    [InlineData("/", "/", "", true)]
    [InlineData("/foo", "/", "foo", true)]
    [InlineData("/foo/bar", "/foo", "bar", true)]
    [InlineData("C:\\", "C:\\", "", false)]
    [InlineData("C:\\foo", "C:\\", "foo", false)]
    [InlineData("C:\\foo\\bar", "C:\\foo", "bar", false)]
    public void Test_GetFullPath(string expected, string? directory, string fileName, bool linux)
        => AssertGetFullPath(expected, directory, fileName, linux);

    [SkippableTheory]
    [InlineData("foo", "foo", true)]
    [InlineData("foo", "foo", false)]
    [InlineData("foo/bar", "foo/bar", true)]
    [InlineData("foo\\bar", "foo\\bar", false)]
    public void Test_GetFullPath_WithoutDirectory(string expected, string fileName, bool linux)
        => AssertGetFullPath(expected, null, fileName, linux);

    [SkippableTheory]
    [InlineData("/", "/", true)]
    [InlineData("C:\\", "C:\\", false)]
    [InlineData("/foo", "/foo", true)]
    [InlineData("C:\\foo", "C:\\foo", false)]
    public void Test_GetFullPath_WithoutFileName(string expected, string directory, bool linux)
        => AssertGetFullPath(expected, directory, "", linux);

    [SkippableTheory]
    [InlineData("/", true)]
    [InlineData("C:\\", false)]
    [InlineData("/foo", true)]
    [InlineData("C:\\foo", false)]
    public void GetFullPath_WithInsufficientBuffer(string input, bool linux)
    {
        Skip.IfNot(linux && OperatingSystem.IsLinux());

        var path = AbsolutePath.FromFullPath(input);
        var insufficientLength = path.GetFullPathLength() - 1;

        path.Invoking(x =>
            {
                Span<char> buffer = stackalloc char[insufficientLength < 0 ? 0 : insufficientLength];
                x.GetFullPath(buffer);
            })
            .Should()
            .Throw<ArgumentException>();
    }

    private static void AssertGetFullPath(string expected, string? directory, string fileName, bool linux)
    {
        Skip.IfNot(linux && OperatingSystem.IsLinux());
        var path = AbsolutePath.FromDirectoryAndFileName(directory, fileName);

        // GetFullPath
        path.GetFullPath().Should().Be(expected);

        // GetFullPath with Span
        Span<char> buffer = stackalloc char[path.GetFullPathLength()];
        path.GetFullPath(buffer);

        buffer.ToString().Should().Be(expected);
    }
}
