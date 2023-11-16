using FluentAssertions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.CLI.Tests.VerbTests;

public class AnalyzeArchive(IServiceProvider provider) : AVerbTest(provider)
{
    [Fact]
    public async Task CanAnalyzeArchives()
    {
        var log = await Run("analyze-archive", "-i", Data7ZipLZMA2.ToString());

        log.Size.Should().Be(1);
        log.LastTable.Columns.Should().BeEquivalentTo("Path", "Size", "Hash");
        log.LastTable.Rows
            .Should()
            .BeEquivalentTo(new[]
            {
                new object[] {"deepFolder/deepFolder2/deepFolder3/deepFolder4/deepFile.txt".ToRelativePath(), (Size)12L, (Hash)0xE405A7CFA6ABBDE3},
                new object[] {"folder1/folder1file.txt".ToRelativePath(), (Size)15L, (Hash)0xC9E47B1523162066},
                new object[] {"rootFile.txt".ToRelativePath(), (Size)12L, (Hash)0x33DDBF7930BA002A}
            });
    }
}
