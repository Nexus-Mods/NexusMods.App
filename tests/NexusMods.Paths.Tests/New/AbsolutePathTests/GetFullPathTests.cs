using NexusMods.Paths.Tests.New.Helpers;

namespace NexusMods.Paths.Tests.New.AbsolutePathTests;

public class GetFullPathTests
{
    [Theory]
    [InlineData("C:/NMA/test.png", "C:/NMA", "test.png")]
    [InlineData("/home/sewer/NMA/test.png", "/home/sewer/NMA", "test.png")]
    public void GetFullPath_Common(string expected, string? directory, string fileName)
    {
        AssertResult(expected, directory, fileName);
    }

    [Theory]
    [InlineData("C:/NMA/test.png", "", "C:/NMA/test.png")]
    [InlineData("/home/sewer/NMA/test.png", "", "/home/sewer/NMA/test.png")]
    public void GetFullPath_WithNullDirectory(string expected, string? directory, string fileName)
    {
        AssertResult(expected, directory, fileName);
    }

    [Theory]
    [InlineData("C:/NMA/test", "", "C:/NMA/test")]
    [InlineData("/home/sewer/NMA/test", "", "/home/sewer/NMA/test")]
    public void GetFullPath_WithEmptyDirectory(string expected, string? directory, string fileName)
    {
        AssertResult(expected, directory, fileName);
    }

    [Theory]
    [InlineData("C:/NMA/test", "C:/NMA/test", "")]
    [InlineData("/home/sewer/NMA/test", "/home/sewer/NMA/test", "")]
    public void GetFullPath_WithDirectoryOnly(string expected, string? directory, string fileName)
    {
        AssertResult(expected, directory, fileName);
    }

    private static void AssertResult(string expected, string? directory, string fileName)
    {
        expected = expected.NormalizeSeparator();
        directory = directory.NormalizeSeparator();
        fileName = fileName.NormalizeSeparator();
        Assert.Equal(expected, AbsolutePath.FromDirectoryAndFileName(directory, fileName).GetFullPath());
    }
}
