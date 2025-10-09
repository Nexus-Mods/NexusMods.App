using FluentAssertions;
using NexusMods.Backend.RuntimeDependency;

namespace NexusMods.CrossPlatform.Tests;

public class UpdateDesktopDatabaseDependencyTests
{
    [Theory]
    [InlineData("update-desktop-database 0.27\n", "0.27")]
    [InlineData("update-desktop-database 0.27", "0.27")]
    [InlineData("update-desktop-database", null)]
    public void TestTryParseVersion(string input, string? expectedRawVersion)
    {
        _ = UpdateDesktopDatabaseDependency.TryParseVersion(input, out var rawVersion, out _);
        rawVersion.Should().Be(expectedRawVersion);
    }
}
