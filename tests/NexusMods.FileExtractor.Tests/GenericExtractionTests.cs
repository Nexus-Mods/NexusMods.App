using FluentAssertions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using Xunit;

namespace NexusMods.FileExtractor;

public class GenericExtractionTests
{
    private readonly FileExtractor _extractor;

    public GenericExtractionTests(FileExtractor extractor)
    {
        _extractor = extractor;
    }
    
    [Theory]
    [MemberData(nameof(Archives))]
    public async Task CanForEachOverFiles(AbsolutePath path)
    {
        var file = KnownFolders.CurrentDirectory.Combine("Resources/data_7zip_lzma2.7z");
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

    public static IEnumerable<object[]> Archives => KnownFolders.CurrentDirectory.Combine("Resources").EnumerateFiles()
        .Select(file => new object[] { file });
}