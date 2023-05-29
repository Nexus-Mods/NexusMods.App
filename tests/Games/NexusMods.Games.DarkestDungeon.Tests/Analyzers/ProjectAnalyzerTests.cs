using System.Xml.Serialization;
using AutoFixture.Xunit2;
using FluentAssertions;
using NexusMods.Games.DarkestDungeon.Analyzers;
using NexusMods.Games.DarkestDungeon.Models;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.DarkestDungeon.Tests.Analyzers;

public class ProjectAnalyzerTests : AFileAnalyzerTest<DarkestDungeon, ProjectAnalyzer>
{
    public ProjectAnalyzerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Theory, AutoData]
    public async Task Test_Analyze(ModProject expected)
    {
        await using var testFile = await CreateTestFile("project.xml", Array.Empty<byte>());
        await using (var stream = testFile.Path.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
        {
            new XmlSerializer(typeof(ModProject)).Serialize(stream, expected);
        }

        var res = await AnalyzeFile(testFile.Path);
        res
            .Should().ContainSingle()
            .Which.Should()
            .BeOfType<ModProject>()
            .Which.Should()
            .Be(expected);
    }
}
