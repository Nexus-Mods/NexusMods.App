using System.Runtime.InteropServices;
using FluentAssertions;

namespace NexusMods.Paths.Tests.New.AbsolutePathTests;

public class ConstructorTests
{
    [Theory]
    [InlineData("C:\foo", "C:", "foo")]
    [InlineData("C:\foo\bar", "C:\foo", "bar")]
    public void Test_Constructor_Windows(string fullPath, string directory, string fileName)
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        var path = new AbsolutePath(directory, fileName);
        path.Directory.Should().Be(directory);
        path.FileName.Should().Be(fileName);
        path.GetFullPath().Should().Be(fullPath);
    }

    [Theory]
    [InlineData("/foo", "/", "foo")]
    [InlineData("/foo/bar", "/foo", "bar")]
    public void Test_Constructor_Linux(string fullPath, string directory, string fileName)
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
        var path = new AbsolutePath(directory, fileName);
        path.Directory.Should().Be(directory);
        path.FileName.Should().Be(fileName);
        path.GetFullPath().Should().Be(fullPath);
    }
}
