using FluentAssertions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.CLI.Tests.VerbTests;

public class HashFolderTests(IServiceProvider provider) : AVerbTest(provider)
{
    [Fact]
    public async Task CanHashFolder()
    {
        await using var folder = TemporaryFileManager.CreateFolder();
        await folder.Path.Combine("file1.txt").WriteAllTextAsync("file1.txt");
        await folder.Path.Combine("file2.txt").WriteAllTextAsync("file2.txt");
        var log = await Run("hash-folder", "-i", folder.Path.ToString());

        log.LastTable.Rows.Count().Should().Be(2);
        log.LastTableColumns.Should().BeEquivalentTo("Path", "Hash", "Size");
        log.LastCellsAsStrings()
            .Should()
            .BeEquivalentTo(new[]
            {
                new object[] {"file1.txt", "0x3F599001C5030E32", "9 B"},
                new object[] {"file2.txt", "0xBD98CD6645E0DF6D", "9 B"},
            });
    }


}
