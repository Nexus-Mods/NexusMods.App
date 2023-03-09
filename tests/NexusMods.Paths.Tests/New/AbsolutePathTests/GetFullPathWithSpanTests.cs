using NexusMods.Paths.Tests.New.Helpers;

namespace NexusMods.Paths.Tests.New.AbsolutePathTests;

public class GetFullPathWithSpanTests
{
    [Theory]
    [InlineData("/foo", "/", "foo")]
    [InlineData("C:/NMA/test.png", "C:/NMA", "test.png")]
    [InlineData("/home/sewer/NMA/test.png", "/home/sewer/NMA", "test.png")]
    public void GetFullPath_Common(string expected, string? directory, string fileName) => AssertGetFullPath(expected, directory, fileName);

    [Theory]
    [InlineData("C:/NMA/test.png", "", "C:/NMA/test.png")]
    [InlineData("/home/sewer/NMA/test.png", "", "/home/sewer/NMA/test.png")]
    public void GetFullPath_WithNullDirectory(string expected, string? directory, string fileName) => AssertGetFullPath(expected, directory, fileName);

    [Theory]
    [InlineData("C:/NMA/test", "", "C:/NMA/test")]
    [InlineData("/home/sewer/NMA/test", "", "/home/sewer/NMA/test")]
    public void GetFullPath_WithEmptyDirectory(string expected, string? directory, string fileName) => AssertGetFullPath(expected, directory, fileName);

    [Theory]
    [InlineData("C:/NMA/test", "C:/NMA/test", "")]
    [InlineData("/home/sewer/NMA/test", "/home/sewer/NMA/test", "")]
    public void GetFullPath_WithDirectoryOnly(string expected, string? directory, string fileName) => AssertGetFullPath(expected, directory, fileName);

    [Theory]
    [InlineData("/foo.txt", "/", "foo.txt")]
    [InlineData("C:/NMA/test.png", "C:/NMA", "test.png")]
    [InlineData("/home/sewer/NMA/test.png", "/home/sewer/NMA", "test.png")]
    public void GetFullPath_WithInsufficientBuffer(string expected, string? directory, string fileName)
    {
        // Insufficient buffer means we return empty string.
        var path = AbsolutePath.FromDirectoryAndFileName(directory.NormalizeSeparator(), fileName.NormalizeSeparator());
        Span<char> buffer = stackalloc char[path.GetFullPathLength() - 1];
        var result = path.GetFullPath(buffer);
        Assert.NotEqual(expected.NormalizeSeparator(), result.ToString());
        Assert.Equal(0, result.Length);
    }

    private static void AssertGetFullPath(string expected, string? directory, string fileName)
    {
        var path = AbsolutePath.FromDirectoryAndFileName(directory.NormalizeSeparator(), fileName.NormalizeSeparator());
        Span<char> buffer = stackalloc char[path.GetFullPathLength()];
        Assert.Equal(expected.NormalizeSeparator(), path.GetFullPath(buffer).ToString());
    }
}
