using FluentAssertions;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.DataModel.Tests;

public class ArchiveManagerTests
{
    private readonly IArchiveManager _manager;
    private readonly TemporaryFileManager _temporaryFileManager;

    public ArchiveManagerTests(IArchiveManager manager, TemporaryFileManager temporaryFileManager)
    {
        _manager = manager;
        _temporaryFileManager = temporaryFileManager;
    }

    [Theory]
    [InlineData(1, 1024 * 1024)]
    [InlineData(24, 1024)]
    [InlineData(124, 1024)]
    public async Task CanArchiveFiles(int fileCount, int maxSize)
    {
        // Randomly generate some file sizes
        var sizes = Enumerable.Range(0, fileCount).Select(s => Size.FromLong(Random.Shared.Next(maxSize))).ToArray();

        // Randomly generate some data
        var datas = sizes.Select(size =>
        {
            var buf = new byte[size.Value];
            Random.Shared.NextBytes(buf);
            return buf;
        }).ToArray();

        // Calculate the hashes
        var hashes = datas.Select(d => d.AsSpan().XxHash64()).ToArray();

        // Create the tuples for compression
        var records = Enumerable.Range(0, fileCount).Select(idx => (
            new ArchivedFileEntry(new MemoryStreamFactory($"{idx}.txt".ToRelativePath(), new MemoryStream(datas[idx])),
            hashes[idx], Size.FromLong(datas[idx].Length))));

        // Backup the files
        await _manager.BackupFiles(records);

        // Verify the files exist in the archive manager
        foreach (var hash in hashes)
            (await _manager.HaveFile(hash)).Should().BeTrue();

        // Extract some of the files
        var extractionCount = Random.Shared.Next(fileCount);
        var extractionIdxs = Enumerable.Range(0, extractionCount).Select(_ => Random.Shared.Next(fileCount)).ToArray();

        // Extract the files via the in-memory method
        var extracted = await _manager.ExtractFiles(extractionIdxs.Select(idx => hashes[idx]));

        // Verify the extracted files are correct
        foreach (var idx in extractionIdxs)
            extracted[hashes[idx]].Should().BeEquivalentTo(datas[idx]);

        // Extract the files via the file method
        await using var tempFolder = _temporaryFileManager.CreateFolder();

        var fullPaths = extractionIdxs.Distinct().ToDictionary(idx => idx, idx => tempFolder.Path.Combine($"{idx}.dat"));

        await _manager.ExtractFiles(extractionIdxs.Select(idx => (hashes[idx], fullPaths[idx])));

        // Verify the extracted files are correct
        foreach (var idx in extractionIdxs)
        {
            var data = await fullPaths[idx].ReadAllBytesAsync();

            data.Should().BeEquivalentTo(datas[idx]);
        }
    }
}
