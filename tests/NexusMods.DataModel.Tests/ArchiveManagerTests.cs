using FluentAssertions;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.DataModel.Tests;

public class ArchiveManagerTests
{
    private readonly IFileStore _manager;
    private readonly TemporaryFileManager _temporaryFileManager;

    public ArchiveManagerTests(IFileStore manager, TemporaryFileManager temporaryFileManager)
    {
        _manager = manager;
        _temporaryFileManager = temporaryFileManager;
    }

    [Theory]
    [InlineData(2, 3)]
    [InlineData(1, 1024 * 1024)]
    [InlineData(5, 1024 * 1024)]
    [InlineData(5, 128)]
    [InlineData(24, 1024)]
    [InlineData(124, 1024)]
    public async Task CanArchiveFiles(int fileCount, int maxSize)
    {
        // Randomly generate some file sizes
        var sizes = Enumerable.Range(1, fileCount).Select(s => Size.FromLong(Random.Shared.Next(1, maxSize))).ToArray();

        // Randomly generate some data
        var datas = sizes.Select(size =>
        {
            var buf = new byte[size.Value];
            Random.Shared.NextBytes(buf);
            return buf;
        }).ToArray();

        // Calculate the hashes
        var hashes = datas.Select(d => d.AsSpan().xxHash3()).ToArray();

        // Create the tuples for compression
        var records = Enumerable.Range(0, fileCount).Select(idx => (
            new ArchivedFileEntry(new MemoryStreamFactory(RelativePath.FromUnsanitizedInput($"{idx}.txt"), new MemoryStream(datas[idx])),
            hashes[idx], Size.FromLong(datas[idx].Length))));

        // Backup the files
        await _manager.BackupFiles(records);

        // Verify the files exist in the file store
        foreach (var hash in hashes)
            (await _manager.HaveFile(hash)).Should().BeTrue();

        // Extract some of the files
        var extractionCount = Random.Shared.Next(fileCount);
        var extractionIdxs = Enumerable.Range(1, extractionCount).Select(_ => Random.Shared.Next(fileCount)).Distinct().ToArray();

        // Extract the files via the in-memory method
        var extracted = await _manager.ExtractFiles(extractionIdxs.Select(idx => hashes[idx]));

        // Verify the extracted files are correct
        foreach (var idx in extractionIdxs)
        {
            extracted[hashes[idx]].Length.Should().Be(datas[idx].Length,
                "the extracted file should have the same length as the original file");
            extracted[hashes[idx]].Should().BeEquivalentTo(datas[idx],
                "the extracted file should be the same as the original file");
        }


        // Extract the files via the file method
        await using var tempFolder = _temporaryFileManager.CreateFolder();

        var fullPaths = extractionIdxs.Distinct().ToDictionary(idx => idx, idx => tempFolder.Path.Combine($"{idx}.dat"));

        var files = extractionIdxs.Select(idx => (hashes[idx], fullPaths[idx])).ToArray();
        await _manager.ExtractFiles(files);

        // Verify the extracted files are correct
        foreach (var idx in extractionIdxs)
        {
            var data = await fullPaths[idx].ReadAllBytesAsync();

            data.Should().BeEquivalentTo(datas[idx]);
        }

        foreach (var idx in extractionIdxs)
        {
            await using var stream = await _manager.GetFileStream(hashes[idx]);
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            var read = ms.ToArray();
            read.Length.Should().Be(datas[idx].Length);
            read.Should().BeEquivalentTo(datas[idx]);
        }
    }
}
