using FluentAssertions;
using NexusMods.CrossPlatform.Process;

namespace NexusMods.CrossPlatform.Tests;

public class XDGSettingsDependencyTests
{
    [Theory]
    [InlineData("xdg-settings 1.2.1\n", "1.2.1")]
    [InlineData("xdg-settings 1.2.1", "1.2.1")]
    [InlineData("xdg-settings", null)]
    public void TestTryParseVersion(string input, string? expectedRawVersion)
    {
        _ = XDGSettingsDependency.TryParseVersion(input, out var rawVersion, out _);
        rawVersion.Should().Be(expectedRawVersion);
    }
}
