using FluentAssertions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.FileExtractor.Tests;

public class SevenZipExtractionTests
{
    private FileExtractor _extractor;

    public SevenZipExtractionTests(FileExtractor extractor)
    {
        _extractor = extractor;
    }
    
    [Fact]
    public async Task CanForeachOverFiles()
    {
        var file = KnownFolders.CurrentDirectory.Join("Resources/data_7zip_lzma2.7z");
        var results = await _extractor.ForEachEntry(new NativeFileStreamFactory(file), async (path, e) =>
        {
            await using var fs = await e.GetStream();
            return await fs.Hash(CancellationToken.None);
        }, CancellationToken.None);

        results.Count.Should().Be(3);
        results.OrderBy(r => r.Key)
            .Select(kv => (Path: kv.Key, Hash: kv.Value))
            .Should()
            .BeEquivalentTo(new (RelativePath, Hash)[]
            {
                (@"deepFolder\deepFolder2\deepFolder3\deepFolder4\deepFile.txt".ToRelativePath(), 0xE405A7CFA6ABBDE3),
                (@"folder1\folder1file.txt".ToRelativePath(), 0xC9E47B1523162066),
                (@"rootFile.txt".ToRelativePath(), 0x33DDBF7930BA002A),
            });
    }
}