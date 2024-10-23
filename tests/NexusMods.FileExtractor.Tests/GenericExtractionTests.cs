using FluentAssertions;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Extensions.BCL;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash3;
using NexusMods.Hashing.xxHash3.Paths;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

// ReSharper disable AccessToDisposedClosure

namespace NexusMods.FileExtractor.Tests;

public class GenericExtractionTests
{
    private readonly IFileExtractor _extractor;
    private readonly TemporaryFileManager _temporaryFileManager;

    public GenericExtractionTests(IFileExtractor extractor, TemporaryFileManager temporaryFileManager)
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
            return await fs.XxHash3Async(CancellationToken.None);
        }, CancellationToken.None);

        results.Count.Should().Be(3);
        results.OrderBy(r => r.Key)
            .Select(kv => (Path: kv.Key, Hash: kv.Value))
            .Should()
            .BeEquivalentTo(new[]
            {
                ("deepFolder/deepFolder2/deepFolder3/deepFolder4/deepFile.txt".ToRelativePath(), (Hash)0x3F0AB4D495E35A9A),
                ("folder1/folder1file.txt".ToRelativePath(), (Hash)0x8520436F06348939),
                ("rootFile.txt".ToRelativePath(), (Hash)0x818A82701BC1CC30),
            });
    }

    [Theory]
    [MemberData(nameof(Archives))]
    public async Task CanExtractAll(AbsolutePath path)
    {
        await using var tempFolder = _temporaryFileManager.CreateFolder();
        await _extractor.ExtractAllAsync(path, tempFolder, CancellationToken.None);
        (await tempFolder.Path.EnumerateFiles()
            .SelectAsync(async f => (f.RelativeTo(tempFolder.Path), await f.XxHash3Async()))
            .ToArrayAsync())
            .Should()
            .BeEquivalentTo([
                ("deepFolder/deepFolder2/deepFolder3/deepFolder4/deepFile.txt".ToRelativePath(), (Hash)0x3F0AB4D495E35A9A),
                ("folder1/folder1file.txt".ToRelativePath(), (Hash)0x8520436F06348939),
                ("rootFile.txt".ToRelativePath(), (Hash)0x818A82701BC1CC30),
                ]
            );
    }

    public static IEnumerable<object[]> Archives => FileSystem.Shared.GetKnownPath(KnownPath.CurrentDirectory).Combine("Resources").EnumerateFiles()
        .Select(file => new object[] { file });
}
