using System.Runtime.InteropServices;
using FluentAssertions;

namespace NexusMods.Paths.Tests.New.AbsolutePathTests;

public class ConstructorTests
{
    [SkippableTheory]
    [InlineData("C:", "", "C:", "", "C:")]
    [InlineData("C:", "foo", "C:", "foo", "C:\\foo")]
    [InlineData("C:\\foo", "bar", "C:\\foo", "bar", "C:\\foo\\bar")]
    [InlineData("C:\\foo\\", "bar", "C:\\foo", "bar", "C:\\foo\\bar")]
    public void Test_Constructor_Windows(string inputDirectory, string inputFileName,
        string expectedDirectory, string expectedFileName, string expectedFullPath)
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        var path = new AbsolutePath(inputDirectory, inputFileName, new InMemoryFileSystem());
        path.Directory.Should().Be(expectedDirectory);
        path.FileName.Should().Be(expectedFileName);
        path.GetFullPath().Should().Be(expectedFullPath);
    }

    [SkippableTheory]
    [InlineData("/", "", "/", "", "/")]
    [InlineData("/", "foo", "/", "foo", "/foo")]
    [InlineData("/foo", "bar", "/foo", "bar", "/foo/bar")]
    [InlineData("/foo/", "bar", "/foo", "bar", "/foo/bar")]
    public void Test_Constructor_Linux(string inputDirectory, string inputFileName,
        string expectedDirectory, string expectedFileName, string expectedFullPath)
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
        var path = new AbsolutePath(inputDirectory, inputFileName, new InMemoryFileSystem());
        path.Directory.Should().Be(expectedDirectory);
        path.FileName.Should().Be(expectedFileName);
        path.GetFullPath().Should().Be(expectedFullPath);
    }
}
