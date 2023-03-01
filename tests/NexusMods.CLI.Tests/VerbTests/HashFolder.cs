using FluentAssertions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.CLI.Tests.VerbTests;

public class HashFolderTests : AVerbTest
{
    public HashFolderTests(TemporaryFileManager temporaryFileManager, IServiceProvider provider) 
        : base(temporaryFileManager, provider)
    {
    }
    
    [Fact]
    public async Task CanHashFolder()
    {
        await using var folder = TemporaryFileManager.CreateFolder();
        await folder.Path.CombineUnchecked("file1.txt").WriteAllTextAsync("file1.txt");
        await folder.Path.CombineUnchecked("file2.txt").WriteAllTextAsync("file2.txt");
        await RunNoBanner("hash-folder", "-f", folder.Path.ToString());

        LogSize.Should().Be(1);
        LastTable.Rows.Count().Should().Be(2);
        LastTable.Columns.Should().BeEquivalentTo("Path", "Hash", "Size", "LastModified");
        LastTable.Rows
            .Select(r => r.Take(3))
            .Should()
            .BeEquivalentTo(new[]
            {
                new object[] {"file1.txt".ToRelativePath(), (Hash)0x3F599001C5030E32, (Size)9L},
                new object[] {"file2.txt".ToRelativePath(), (Hash)0xBD98CD6645E0DF6D, (Size)9L},
            });
    }


}