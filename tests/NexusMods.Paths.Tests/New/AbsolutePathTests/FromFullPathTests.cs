using System.Runtime.InteropServices;
using FluentAssertions;

namespace NexusMods.Paths.Tests.New.AbsolutePathTests;

public class FromFullPathTests
{
    [Theory]
    [InlineData("C:\foo")]
    [InlineData("C:\foo\bar")]
    [InlineData("C:\foo\bar\baz")]
    public void Test_FromFullPath_Windows(string fullPath)
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        AbsolutePath.FromFullPath(fullPath).GetFullPath().Should().Be(fullPath);
    }

    [Theory]
    [InlineData("/foo")]
    [InlineData("/foo/bar")]
    [InlineData("/foo/bar/baz")]
    public void Test_FromFullPath_Linux(string fullPath)
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
        AbsolutePath.FromFullPath(fullPath).GetFullPath().Should().Be(fullPath);
    }
}
