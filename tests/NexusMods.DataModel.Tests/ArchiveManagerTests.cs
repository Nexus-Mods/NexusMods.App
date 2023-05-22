using System.Text;
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

    public ArchiveManagerTests(IArchiveManager manager, TemporaryFileManager temporaryFileManager)
    {
        _manager = manager;
    }

    [Theory]
    [InlineData(1, 1024 * 1024)]
    [InlineData(24, 1024)]
    [InlineData(124, 1024)]
    public async Task CanArchiveFiles(int fileCount, int maxSize)
    {
        var sizes = Enumerable.Range(0, fileCount).Select(s => Size.FromLong(Random.Shared.Next(maxSize))).ToArray();
        var datas = sizes.Select(size =>
        {
            var buf = new byte[size.Value];
            Random.Shared.NextBytes(buf);
            return buf;
        }).ToArray();
        var hashes = datas.Select(d => d.AsSpan().XxHash64()).ToArray();

        var records = Enumerable.Range(0, fileCount).Select(idx => (
            (IStreamFactory)new MemoryStreamFactory($"{idx}.txt".ToRelativePath(), new MemoryStream(datas[idx])),
            hashes[idx], Size.FromLong(datas[idx].Length)));

        await _manager.BackupFiles(records);

        foreach (var hash in hashes)
            (await _manager.HaveFile(hash)).Should().BeTrue();
    }
    

}
