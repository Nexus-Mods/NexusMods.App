using FluentAssertions;
using NexusMods.CrossPlatform.Process;

namespace NexusMods.CrossPlatform.Tests;

public class XDGOpenDependencyTests
{
    [Theory]
    [InlineData("xdg-open 1.2.1\n", "1.2.1")]
    [InlineData("xdg-open 1.2.1", "1.2.1")]
    [InlineData("xdg-open", null)]
    public void TestTryParseVersion(string input, string? expectedRawVersion)
    {
        _ = XDGOpenDependency.TryParseVersion(input, out var rawVersion, out _);
        rawVersion.Should().Be(expectedRawVersion);
    }
}
