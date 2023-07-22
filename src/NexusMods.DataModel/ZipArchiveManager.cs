using System.Buffers.Binary;
using System.IO.Compression;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveContents;
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
        var outputPath = _archiveLocations.First().Combine(id).AppendExtension(KnownExtensions.Tmp);
        {
            await using var archiveStream = outputPath.Create();
            using var builder = new ZipArchive(archiveStream, ZipArchiveMode.Create, true, System.Text.Encoding.UTF8);

            foreach (var backup in distinct)
            {
                await using var srcStream = await backup.Item1.GetStreamAsync();
                var entry = builder.CreateEntry(backup.Item2.ToHex());
                await using var entryStream = entry.Open();
                await srcStream.CopyToAsync(entryStream, token);
                await entryStream.FlushAsync(token);
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
        var grouped = files.Distinct()
            .Select(input => TryGetLocation(input.Src, out var archivePath)
                ? (true, Hash:input.Src, ArchivePath:archivePath, input.Dest)
                : default)
            .Where(x => x.Item1)
            .ToLookup(l => l.ArchivePath, l => (l.Hash, l.Dest));

        if (grouped[default].Any())
            throw new Exception($"Missing archive for {grouped[default].First().Hash.ToHex()}");


        foreach (var group in grouped)
        {
            await using var file = group.Key.Read();
            using var archive = new ZipArchive(file, ZipArchiveMode.Read, true, System.Text.Encoding.UTF8);

            var toExtract = group.ToLookup(x => x.Hash.ToHex());

            foreach (var entry in archive.Entries)
            {
                if (!toExtract.Contains(entry.Name))
                    continue;

                foreach (var (_, dest) in toExtract[entry.Name])
                {
                    if (!dest.Parent.DirectoryExists())
                        dest.Parent.CreateDirectory();
                    await using var entryStream = entry.Open();
                    await using var destStream = dest.Create();
                    await entryStream.CopyToAsync(destStream, token);
                    await destStream.FlushAsync(token);
                }
            }
        }
    }

    /// <inheritdoc/>
    public async Task<IDictionary<Hash, byte[]>> ExtractFiles(IEnumerable<Hash> files, CancellationToken token = default)
    {
        var results = new Dictionary<Hash, byte[]>();

        var grouped = files.Distinct()
            .Select(hash => TryGetLocation(hash, out var archivePath)
                ? (true, Hash:hash, ArchivePath:archivePath)
                : default)
            .Where(x => x.Item1)
            .ToLookup(l => l.ArchivePath, l => l.Hash);

        if (grouped[default].Any())
            throw new Exception($"Missing archive for {grouped[default].First().ToHex()}");
        
        foreach (var group in grouped)
        {
            var byHash = group.ToDictionary(f => f.ToHex());
            await using var file = group.Key.Read();

            using var srcArchive = new ZipArchive(file, ZipArchiveMode.Read, true, System.Text.Encoding.UTF8);

            foreach (var entry in srcArchive.Entries)
            {
                if (!byHash.TryGetValue(entry.Name, out var hash))
                    continue;

                await using var entryStream = entry.Open();
                var ms = new MemoryStream();
                await entryStream.CopyToAsync(ms, token);
                results[hash] = ms.ToArray();
            }
        }

        return results;
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
