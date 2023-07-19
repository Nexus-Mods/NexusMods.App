using System.Text.Json;
using FluentAssertions;
using NexusMods.Games.StardewValley.Models;

namespace NexusMods.Games.StardewValley.Tests.Models;

// ReSharper disable InconsistentNaming

public class SMAPIVersionTests
{
    [Theory]
    [InlineData("1.2", 1, 2, 0, 0, null, null)]
    [InlineData("123.456", 123, 456, 0, 0, null, null)]
    [InlineData("1.0.2", 1, 0, 2, 0, null, null)]
    [InlineData("1.2.3", 1, 2, 3, 0, null, null)]
    [InlineData("12.34.56", 12, 34, 56, 0, null, null)]
    [InlineData("1.2-beta123", 1, 2, 0, 0, "beta123", null)]
    [InlineData("1.2-beta123+debug5", 1, 2, 0, 0, "beta123", "debug5")]
    [InlineData("1.2+debug4", 1, 2, 0, 0, null, "debug4")]
    public void Test_TryParse_Success(
        string input,
        int expectedMajorVersion,
        int expectedMinorVersion,
        int expectedPatchVersion,
        int expectedPlatformRelease,
        string? expectedPrereleaseTag,
        string? expectedBuildMetadata)
    {
        var res = SMAPIVersion.TryParse(input, out var version);
        res.Should().BeTrue();
        version.Should().NotBeNull();
        version!.MajorVersion.Should().Be(expectedMajorVersion);
        version.MinorVersion.Should().Be(expectedMinorVersion);
        version.PatchVersion.Should().Be(expectedPatchVersion);
        version.PlatformRelease.Should().Be(expectedPlatformRelease);
        version.PrereleaseTag.Should().Be(expectedPrereleaseTag);
        version.BuildMetadata.Should().Be(expectedBuildMetadata);

        version.ToString().Should().Be(input);

        var json = JsonSerializer.Serialize(version);
        var deserialized = JsonSerializer.Deserialize<SMAPIVersion>(json);
        deserialized.Should().Be(version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("1.2:debug")]
    [InlineData("01.02")]
    public void Test_Try_Parse_Failure(string input)
    {
        var res = SMAPIVersion.TryParse(input, out var version);
        version.Should().BeNull();
        res.Should().BeFalse();
    }
}
