using FluentAssertions;
namespace NexusMods.Networking.ModUpdates.Tests;

/// <summary>
/// Tests based around fuzzy searching of updates for files based on filenames.
/// </summary>
public class FuzzySearchTests
{
    [Theory]
    // Reference Example: SkyUI (underscores and version in middle)
    [InlineData("SkyUI_5_2_SE", "5.2SE", "skyui se")]
    // Reference Example: Skyrim 202X (version after name)
    [InlineData("Skyrim 202X 9.0 - Architecture PART 1", "9.0", "skyrim 202x - architecture part 1")]
    // Reference Example: Quality World Map (file version with suffix)
    [InlineData("9.0 A Quality World Map - Paper", "9.0P", "a quality world map - paper")]
    // Reference Example: Maestros of Synth (extension stripping)
    [InlineData("Maestros of Synth.zip", "", "maestros of synth")]
    // USSEP style with underscores and extension
    [InlineData("Unofficial_Skyrim_Special_Edition_Patch.rar", "", "unofficial skyrim special edition patch")]
    // Multiple spaces normalization
    [InlineData("Mod    Name  with   multiple    spaces", "", "mod name with multiple spaces")]
    // Version with non-semver beta suffix
    [InlineData("Mod 1.2beta.zip", "1.2beta", "mod")]
    // Version with non-semver suffix and underscores
    [InlineData("Cool_Mod_1_2alpha.7z", "1.2alpha", "cool mod")]
    // (Sewer) Edge cases we don't currently handle:
    // - Version with space between number and suffix in name
    //      - e.g. 'Cool Mod 1.2 alpha.7z' + '1.2alpha' will not eliminate '1.2 alpha', because version doesn't have a space in name.
    public void NormalizeFileName_ShouldHandleVariousCases(string fileName, string version, string expected)
    {
        // Act
        var result = FuzzySearch.NormalizeFileName(fileName, version);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "1.0", "")] // Null input
    [InlineData("test", null, "test")] // Null Version
    [InlineData("", "1.0", "")] // Empty string
    [InlineData(" ", "1.0", "")] // Whitespace only
    public void NormalizeFileName_ShouldHandleEmptyInput(string? fileName, string version, string expected)
    {
        // Act
        var result = FuzzySearch.NormalizeFileName(fileName, version);

        // Assert
        result.Should().Be(expected);
    }
    
    [Theory]
    // Suffix
    [InlineData("5.2SE", new[] { "5.2SE", "5.2", "5_2SE", "5_2" })]
    [InlineData("9.0P", new[] { "9.0P", "9.0", "9_0P", "9_0" })]
    [InlineData("9.0VF", new[] { "9.0VF", "9.0", "9_0VF", "9_0" })]
    // Prefix
    [InlineData("v5.2", new[] { "v5.2", "5.2", "v5_2", "5_2" })]
    // Standard
    [InlineData("10.0.1", new[] { "10.0.1", "10_0_1" })]
    // Invalid
    [InlineData("", new string[] { })]
    [InlineData(null, new string[] { })]
    // Prefix and Suffix
    [InlineData("v1alpha", new[] { "v1alpha", "1alpha", "v1", "1" })]
    [InlineData("v5.2SE", new[] { "v5.2SE", "5.2SE", "v5.2", "5.2", "v5_2SE", "5_2SE", "v5_2", "5_2" })]
    [InlineData("v1.0beta", new[] { "v1.0beta", "1.0beta", "v1.0", "1.0", "v1_0beta", "1_0beta", "v1_0", "1_0" })]
    // SemVer Prereleases
    [InlineData("1.0-alpha", new[] { "1.0-alpha", "1.0", "1_0-alpha", "1_0" })]
    [InlineData("v1.0-alpha", new[] { "v1.0-alpha", "1.0-alpha", "v1.0", "1.0", "v1_0-alpha", "1_0-alpha", "v1_0", "1_0" })]
    public void GetVersionPermutations_ShouldReturnAllPossibleVersionFormats(string input, string[] expected)
    {
        // Act
        var result = FuzzySearch.GetVersionPermutations(input);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("Maestros of Synth.zip", "Maestros of Synth")]
    [InlineData("Maestros of Synth.ZIP", "Maestros of Synth")]
    [InlineData("Maestros of Synth.7z", "Maestros of Synth")]
    [InlineData("Maestros of Synth.omod", "Maestros of Synth")]
    [InlineData("Maestros of Synth", "Maestros of Synth")]
    [InlineData("Something.with.dots.zip", "Something.with.dots")]
    [InlineData("Maestros of Synth.nx", "Maestros of Synth")] // maybe one day
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData(" ", " ")]
    public void StripFileExtensions_ShouldHandleAllowedFileExtension(string input, string expected)
    {
        // Act
        var result = FuzzySearch.StripFileExtension(input);

        // Assert
        result.Should().Be(expected);
    }
}
