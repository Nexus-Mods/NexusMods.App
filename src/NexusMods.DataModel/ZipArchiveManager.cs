using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.ChunkedReaders;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.DataModel;

/// <summary>
/// Archive manager that uses zip files instead of the Nexus Mods archive format. This is used for testing and
/// to verify possible stability and memory issues with the Nexus Mods archive format implementation.
/// </summary>
public class ZipArchiveManager : IArchiveManager
{
    private readonly AbsolutePath[] _archiveLocations;
    private readonly IDataStore _store;

    private const long _chunkSize = 1024 * 1024;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="store"></param>
    /// <param name="settings"></param>
    public ZipArchiveManager(IDataStore store, IDataModelSettings settings)
    {
        _archiveLocations = settings.ArchiveLocations.Select(f => f.ToAbsolutePath()).ToArray();
        foreach (var location in _archiveLocations)
        {
            if (!location.DirectoryExists())
                location.CreateDirectory();
        }
        _store = store;

    }

    /// <inheritdoc/>
    public ValueTask<bool> HaveFile(Hash hash)
    {
        return ValueTask.FromResult(TryGetLocation(hash, out _));
    }

    /// <inheritdoc/>
    public async Task BackupFiles(IEnumerable<(IStreamFactory, Hash, Size)> backups, CancellationToken token = default)
    {
        var guid = Guid.NewGuid();
        var id = guid.ToString();
        var distinct = backups.DistinctBy(d => d.Item2).ToArray();

        using var buffer = MemoryPool<byte>.Shared.Rent((int)_chunkSize);
        var outputPath = _archiveLocations.First().Combine(id).AppendExtension(KnownExtensions.Tmp);
        {
            await using var archiveStream = outputPath.Create();
            using var builder = new ZipArchive(archiveStream, ZipArchiveMode.Create, true, System.Text.Encoding.UTF8);

            foreach (var backup in distinct)
            {
                await using var srcStream = await backup.Item1.GetStreamAsync();
                var chunkCount = (int)(backup.Item3.Value / _chunkSize);
                if (backup.Item3.Value % _chunkSize > 0)
                    chunkCount++;

                var hexName = backup.Item2.ToHex();
                for (var chunkIdx = 0; chunkIdx < chunkCount; chunkIdx++)
                {
                    var entry = builder.CreateEntry($"{hexName}_{chunkIdx}", CompressionLevel.Optimal);
                    await using var entryStream = entry.Open();

                    var toCopy = (int)Math.Min(_chunkSize, (long)backup.Item3.Value - (chunkIdx * _chunkSize));
                    await srcStream.ReadExactlyAsync(buffer.Memory[..toCopy], token);
                    await entryStream.WriteAsync(buffer.Memory[..toCopy], token);
                    await entryStream.FlushAsync(token);
                }
            }
        }

        var finalPath = outputPath.ReplaceExtension(KnownExtensions.Zip);

        await outputPath.MoveToAsync(finalPath, token: token);
        UpdateIndexes(distinct, guid, finalPath);
    }

    private void UpdateIndexes((IStreamFactory, Hash, Size)[] distinct, Guid guid,
        AbsolutePath finalPath)
    {
        foreach (var entry in distinct)
        {
            var dbId = IdFor(entry.Item2, guid);

            var dbEntry = new ArchivedFiles
            {
                File = finalPath.FileName,
                FileEntryData = Array.Empty<byte>()
            };

            // TODO: Consider a bulk-put operation here
            _store.Put(dbId, dbEntry);
        }
    }

    private IId IdFor(Hash hash, Guid guid)
    {
        Span<byte> buffer = stackalloc byte[24];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, hash.Value);
        guid.TryWriteBytes(buffer.SliceFast(8));
        return IId.FromSpan(EntityCategory.ArchivedFiles, buffer);
    }

    /// <inheritdoc/>
    public async Task ExtractFiles(IEnumerable<(Hash Src, AbsolutePath Dest)> files, CancellationToken token = default)
    {
        foreach (var (src, dest) in files)
        {
            await using var srcStream = await GetFileStream(src, token);
            dest.Parent.CreateDirectory();
            await using var destStream = dest.Create();
            await srcStream.CopyToAsync(destStream, token);
        }
    }

    /// <inheritdoc/>
    public async Task<IDictionary<Hash, byte[]>> ExtractFiles(IEnumerable<Hash> files, CancellationToken token = default)
    {
        var results = new Dictionary<Hash, byte[]>();

        foreach (var hash in files.Distinct())
        {
            await using var srcStream = await GetFileStream(hash, token);
            await using var destStream = new MemoryStream();
            await srcStream.CopyToAsync(destStream, token);
            results.Add(hash, destStream.ToArray());
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<Stream> GetFileStream(Hash hash, CancellationToken token = default)
    {
        if (!TryGetLocation(hash, out var archivePath))
            throw new Exception($"Missing archive for {hash.ToHex()}");

        var file = archivePath.Read();
        var archive = new ZipArchive(file, ZipArchiveMode.Read, true, System.Text.Encoding.UTF8);

        return new ChunkedStream(new ChunkedArchiveReader(archive, hash));
    }

    private class ChunkedArchiveReader : IChunkedReaderSource
    {
        private readonly ZipArchiveEntry[] _entries;

        public ChunkedArchiveReader(ZipArchive archive, Hash hash)
        {
            var prefix = hash.ToHex() + "_";
            _entries = archive.Entries.Where(entry => entry.Name.StartsWith(prefix))
                .Order()
                .ToArray();
            Size = Size.FromLong(_entries.Sum(e => e.Length));
            ChunkSize = Size.FromLong(_chunkSize);
            ChunkCount = (ulong)_entries.Length;
        }

        public Size Size { get; }
        public Size ChunkSize { get; }
        public ulong ChunkCount { get; }
        public async Task ReadChunkAsync(Memory<byte> buffer, ulong chunkIndex, CancellationToken token = default)
        {
            await using var stream = _entries[chunkIndex].Open();
            await stream.ReadAtLeastAsync(buffer, buffer.Length, false, token);
        }

        public void ReadChunk(Span<byte> buffer, ulong chunkIndex)
        {
            using var stream = _entries[chunkIndex].Open();
            stream.ReadAtLeast(buffer, buffer.Length, false);
        }
    }

    private bool TryGetLocation(Hash hash, out AbsolutePath archivePath)
    {
        var prefix = new Id64(EntityCategory.ArchivedFiles, (ulong)hash);
        foreach (var entry in _store.GetByPrefix<ArchivedFiles>(prefix))
        {
            foreach (var location in _archiveLocations)
            {
                var path = location.Combine(entry.File);
                if (!path.FileExists) continue;

                archivePath = path;
                return true;
            }

        }

        archivePath = default;
        return false;
    }
}
