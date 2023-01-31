using FluentAssertions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.CLI.Tests.VerbTests;

public class AnalyzeArchive : AVerbTest
{
    public AnalyzeArchive(TemporaryFileManager temporaryFileManager, IServiceProvider provider) : base(temporaryFileManager, provider)
    {
    }

    [Fact]
    public async Task CanAnalyzeArchives()
    {
        await RunNoBanner("analyze-archive", "-i", Data7ZipLZMA2.ToString());

        LogSize.Should().Be(1);
        LastTable.Columns.Should().BeEquivalentTo("Path", "Size", "Hash", "Signatures");
        LastTable.Rows
            .Should()
            .BeEquivalentTo(new[]
            {
                new object[] {@"deepFolder\deepFolder2\deepFolder3\deepFolder4\deepFile.txt".ToRelativePath(), (Size)12L, (Hash)0xE405A7CFA6ABBDE3, "TXT"}, 
                new object[] {@"folder1\folder1file.txt".ToRelativePath(), (Size)15L, (Hash)0xC9E47B1523162066, "TXT"},
                new object[] {@"rootFile.txt".ToRelativePath(), (Size)12L, (Hash)0x33DDBF7930BA002A, "TXT"}
            });
    }
}