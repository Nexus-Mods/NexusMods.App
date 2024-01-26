using FluentAssertions;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Extensions.BCL;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

// ReSharper disable AccessToDisposedClosure

namespace NexusMods.FileExtractor.Tests;

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
        var results = await _extractor.ForEachEntry(new NativeFileStreamFactory(path), async (_, e) =>
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
                ("deepFolder/deepFolder2/deepFolder3/deepFolder4/deepFile.txt".ToRelativePath(), (Hash)0xE405A7CFA6ABBDE3),
                ("folder1/folder1file.txt".ToRelativePath(), (Hash)0xC9E47B1523162066),
                ("rootFile.txt".ToRelativePath(), (Hash)0x33DDBF7930BA002A),
            });
    }

    [Theory]
    [MemberData(nameof(Archives))]
    public async Task CanExtractAll(AbsolutePath path)
    {
        await using var tempFolder = _temporaryFileManager.CreateFolder();
        await _extractor.ExtractAllAsync(path, tempFolder, CancellationToken.None);
        (await tempFolder.Path.EnumerateFiles()
            .SelectAsync(async f => (f.RelativeTo(tempFolder.Path), await f.XxHash64Async()))
            .ToArrayAsync())
            .Should()
            .BeEquivalentTo(new[]
            {
                ("deepFolder/deepFolder2/deepFolder3/deepFolder4/deepFile.txt".ToRelativePath(), (Hash)0xE405A7CFA6ABBDE3),
                ("folder1/folder1file.txt".ToRelativePath(), (Hash)0xC9E47B1523162066),
                ("rootFile.txt".ToRelativePath(), (Hash)0x33DDBF7930BA002A),
            });
    }

    [Theory]
    [MemberData(nameof(Archives))]
    public async Task CanTestForExtractionSupport(AbsolutePath path)
    {
        (await _extractor.CanExtract(path)).Should().BeTrue();
    }

    public static IEnumerable<object[]> Archives => FileSystem.Shared.GetKnownPath(KnownPath.CurrentDirectory).Combine("Resources").EnumerateFiles()
        .Select(file => new object[] { file });
}
