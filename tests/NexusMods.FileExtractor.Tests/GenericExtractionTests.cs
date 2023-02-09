using FluentAssertions;
using NexusMods.Common;
using NexusMods.DataModel.Extensions;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.FileExtractor;

public class GenericExtractionTests
{
    private readonly FileExtractor _extractor;
    private readonly TemporaryFileManager _temporaryFileManager;

    public GenericExtractionTests(FileExtractor extractor, TemporaryFileManager temporaryFileManager)
    {
        _extractor = extractor;
        _temporaryFileManager = temporaryFileManager;
    }
    
    [Theory]
    [MemberData(nameof(Archives))]
    public async Task CanForEachOverFiles(AbsolutePath path)
    {
        var results = await _extractor.ForEachEntry(new NativeFileStreamFactory(path), async (path, e) =>
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
                (@"deepFolder\deepFolder2\deepFolder3\deepFolder4\deepFile.txt".ToRelativePath(), (Hash)0xE405A7CFA6ABBDE3),
                (@"folder1\folder1file.txt".ToRelativePath(), (Hash)0xC9E47B1523162066),
                (@"rootFile.txt".ToRelativePath(), (Hash)0x33DDBF7930BA002A),
            });
    }
    
    [Theory]
    [MemberData(nameof(Archives))]
    public async Task CanExtractAll(AbsolutePath path)
    {
        await using var tempFolder = _temporaryFileManager.CreateFolder();
        await _extractor.ExtractAll(path, tempFolder, CancellationToken.None);
        (await tempFolder.Path.EnumerateFiles()
            .SelectAsync(async f => (f.RelativeTo(tempFolder.Path), await f.XxHash64()))
            .ToArray())
            .Should()
            .BeEquivalentTo(new (RelativePath, Hash)[]
            {
                (@"deepFolder\deepFolder2\deepFolder3\deepFolder4\deepFile.txt".ToRelativePath(), (Hash)0xE405A7CFA6ABBDE3),
                (@"folder1\folder1file.txt".ToRelativePath(), (Hash)0xC9E47B1523162066),
                (@"rootFile.txt".ToRelativePath(), (Hash)0x33DDBF7930BA002A),
            });
    }

    [Theory]
    [MemberData(nameof(Archives))]
    public async Task CanTestForExtractionSupport(AbsolutePath path)
    {
        (await _extractor.CanExtract(path)).Should().BeTrue();
    }

    public static IEnumerable<object[]> Archives => KnownFolders.CurrentDirectory.Join("Resources").EnumerateFiles()
        .Select(file => new object[] { file });
}