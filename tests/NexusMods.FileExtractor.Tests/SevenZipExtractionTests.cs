using FluentAssertions;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.FileExtractor.Tests;

public class SevenZipExtractionTests
{
    private readonly FileExtractor _extractor;

    public SevenZipExtractionTests(FileExtractor extractor)
    {
        _extractor = extractor;
    }

    [Fact]
    public async Task CanForeachOverFiles()
    {
        var file = KnownFolders.CurrentDirectory.CombineUnchecked("Resources/data_7zip_lzma2.7z");
        var results = await _extractor.ForEachEntry(new NativeFileStreamFactory(file), async (_, e) =>
        {
            await using var fs = await e.GetStreamAsync();
            return await fs.XxHash64Async(CancellationToken.None);
        }, CancellationToken.None);

        results.Count.Should().Be(3);
        results.OrderBy(r => r.Key)
            .Select(kv => (Path: kv.Key, Hash: kv.Value))
            .Should()
            .BeEquivalentTo(new[]
            {
                (@"deepFolder\deepFolder2\deepFolder3\deepFolder4\deepFile.txt".ToRelativePath(), (Hash)0xE405A7CFA6ABBDE3),
                (@"folder1\folder1file.txt".ToRelativePath(), (Hash)0xC9E47B1523162066),
                (@"rootFile.txt".ToRelativePath(), (Hash)0x33DDBF7930BA002A),
            });
    }
}
