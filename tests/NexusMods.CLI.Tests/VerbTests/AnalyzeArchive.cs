using FluentAssertions;

namespace NexusMods.CLI.Tests.VerbTests;

public class AnalyzeArchive(IServiceProvider provider) : AVerbTest(provider)
{
    [Fact]
    public async Task CanAnalyzeArchives()
    {
        var log = await Run("analyze-archive", "-i", Data7ZipLZMA2.ToString());

        log.Size.Should().Be(1);
        log.LastTableColumns.Should().BeEquivalentTo("Path", "Size", "Hash");
        log.LastCellsAsStrings()
            .Should()
            .BeEquivalentTo(new[]
            {
                new object[] {"deepFolder/deepFolder2/deepFolder3/deepFolder4/deepFile.txt", "12 B", "0xE405A7CFA6ABBDE3"},
                new object[] {"folder1/folder1file.txt", "15 B", "0xC9E47B1523162066"},
                new object[] {"rootFile.txt", "12 B", "0x33DDBF7930BA002A"}
            });
    }
}
