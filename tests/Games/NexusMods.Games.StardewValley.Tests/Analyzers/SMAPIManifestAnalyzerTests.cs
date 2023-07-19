using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using FluentAssertions;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Games.StardewValley.Analyzers;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.StardewValley.Tests.Analyzers;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class SMAPIManifestAnalyzerTests : AFileAnalyzerTest<StardewValley, SMAPIManifestAnalyzer>
{
    public SMAPIManifestAnalyzerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Fact]
    public void Test_FileTypes()
    {
        FileAnalyzer.FileTypes.Should().ContainSingle(x => x == FileType.JSON);
    }

    [Fact]
    public async Task Test_Analyze()
    {
        var expected = new SMAPIManifest
        {
            Name = Guid.NewGuid().ToString("N"),
            Version = new SMAPIVersion
            {
                MajorVersion = 1,
                MinorVersion = 2,
            },
            UniqueID = Guid.NewGuid().ToString("N"),
            MinimumApiVersion = new SMAPIVersion
            {
                MajorVersion = 3,
                MinorVersion = 12
            }
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(expected);
        await using var path = await CreateTestFile("manifest.json", bytes);

        var result = await AnalyzeFile(path.Path);
        result
            .Should().ContainSingle()
            .Which
            .Should().BeOfType<SMAPIManifest>()
            .Which
            .Should().Be(expected);
    }

    [Fact]
    public async Task Test_Dependencies()
    {

        var expected = new SMAPIManifest
        {
            Name = "foo",
            Version = new SMAPIVersion
            {
                MajorVersion = 1,
                MinorVersion = 0,
                PatchVersion = 5
            },
            UniqueID = "foo",
            MinimumApiVersion = new SMAPIVersion
            {
                MajorVersion = 3,
                MinorVersion = 12
            },
            Dependencies = new SMAPIManifestDependency[]
            {
                new()
                {
                    UniqueID = "bar",
                }
            }
        };

        const string input = @"
{
    ""Name"": ""foo"",
    ""UniqueID"": ""foo"",
    ""Version"": ""1.0.5"",
    ""Description"": ""This is a cool description."",
    ""EntryDll"": ""foo.dll"",
    ""MinimumApiVersion"": ""3.12.0"",
    ""UpdateKeys"": [""Nexus:0""],
    ""Dependencies"": [
        {
            /* this is a comment */
            ""UniqueID"": ""bar"",
        }
    ]
}
";

        await using var path = await CreateTestFile("manifest.json", input);

        var result = await AnalyzeFile(path);
        result
            .Should().ContainSingle()
            .Which
            .Should().BeEquivalentTo(expected);
    }
}
