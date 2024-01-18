using FluentAssertions;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.BCL.Extensions;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.FileExtractor.Tests;

public class SevenZipExtractionTests
{
    private readonly FileExtractor _extractor;

    private readonly TemporaryFileManager _temporaryFileManager;

    private readonly IFileSystem _fileSystem;

    public SevenZipExtractionTests(FileExtractor extractor, TemporaryFileManager temporaryFileManager,
        IFileSystem fileSystem)
    {
        _extractor = extractor;
        _temporaryFileManager = temporaryFileManager;
        _fileSystem = fileSystem;
    }

    [Fact]
    public async Task CanExtractToLongPath()
    {
        await using var tempFolder = _temporaryFileManager.CreateFolder();
        var dest = tempFolder.Path;

        // Create a long path
        while (!(dest.GetFullPathLength() > 280))
        {
            dest = dest.Combine("subfolder");
            _fileSystem.CreateDirectory(dest);
        }

        dest.GetFullPathLength().Should().BeGreaterThan(280);

        _fileSystem.CreateDirectory(dest);

        var file = FileSystem.Shared.GetKnownPath(KnownPath.CurrentDirectory)
            .Combine("Resources/data_7zip_lzma2.7z");

        var act = async () => await _extractor.ExtractAllAsync(file, dest, CancellationToken.None);

        await act.Should().NotThrowAsync();


        (await tempFolder.Path.EnumerateFiles()
                .SelectAsync(async f => (f.RelativeTo(dest), await f.XxHash64Async()))
                .ToArrayAsync())
            .Should()
            .BeEquivalentTo(new[]
            {
                ("deepFolder/deepFolder2/deepFolder3/deepFolder4/deepFile.txt".ToRelativePath(), (Hash)0xE405A7CFA6ABBDE3),
                ("folder1/folder1file.txt".ToRelativePath(), (Hash)0xC9E47B1523162066),
                ("rootFile.txt".ToRelativePath(), (Hash)0x33DDBF7930BA002A),
            });


    }

    [Fact]
    public async Task CanForeachOverFiles()
    {
        var file = FileSystem.Shared.GetKnownPath(KnownPath.CurrentDirectory).Combine("Resources/data_7zip_lzma2.7z");
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
                ("deepFolder/deepFolder2/deepFolder3/deepFolder4/deepFile.txt".ToRelativePath(), (Hash)0xE405A7CFA6ABBDE3),
                ("folder1/folder1file.txt".ToRelativePath(), (Hash)0xC9E47B1523162066),
                ("rootFile.txt".ToRelativePath(), (Hash)0x33DDBF7930BA002A),
            });
    }
}
