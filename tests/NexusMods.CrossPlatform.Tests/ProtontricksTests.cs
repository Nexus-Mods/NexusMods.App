using FluentAssertions;
using NexusMods.Games.Generic.Dependencies;

namespace NexusMods.CrossPlatform.Tests;

public class ProtontricksTests
{
    [Theory]
    [InlineData("protontricks (1.11.1)\n", "1.11.1")]
    [InlineData("protontricks (1.11.1)", "1.11.1")]
    [InlineData("protontricks", null)]
    public void TestTryParseVersion(string input, string? expectedRawVersion)
    {
        _ = ProtontricksNativeDependency.TryParseVersion(input, out var rawVersion, out _);
        rawVersion.Should().Be(expectedRawVersion);
    }
}
