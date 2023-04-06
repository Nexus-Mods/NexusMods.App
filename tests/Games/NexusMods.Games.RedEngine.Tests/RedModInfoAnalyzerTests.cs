using FluentAssertions;
using NexusMods.Games.RedEngine.FileAnalyzers;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Tests;

public class RedModInfoAnalyzerTests : AFileAnalyzerTest<Cyberpunk2077, RedModInfoAnalyzer>
{
    public RedModInfoAnalyzerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Theory]
    [InlineData("foo")]
    public async Task Test_FileAnalyzer(string name)
    {
        var contents = $@"{{ ""name"": ""{name}"" }}";

        await using var file = await CreateTestFile(contents, new Extension(".json"));
        var res = await AnalyzeFile(file.Path);
        res
            .Should().ContainSingle()
            .Which
            .Should().BeOfType<RedModInfo>()
            .Which.Name
            .Should().Be(name);
    }
}
