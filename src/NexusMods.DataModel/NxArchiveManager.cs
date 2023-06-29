using System.Buffers.Binary;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Utilities;
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
/// Manages the archive locations and allows for the backup of files to internal data folders.
/// </summary>
public class NxArchiveManager : IArchiveManager
{
    private readonly AbsolutePath[] _archiveLocations;
    private readonly IDataStore _store;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="store"></param>
    /// <param name="settings"></param>
    public NxArchiveManager(IDataStore store, IDataModelSettings settings)
    {
        _archiveLocations = settings.ArchiveLocations.Select(f => f.ToAbsolutePath()).ToArray();
        foreach (var location in _archiveLocations)
        {
            if (!location.DirectoryExists())
                location.CreateDirectory();
        }
        _store = store;

    }

    /// <inheritdoc />
    public ValueTask<bool> HaveFile(Hash hash)
    {
        return ValueTask.FromResult(TryGetLocation(hash, out _, out _));
    }

    /// <inheritdoc />
    public async Task BackupFiles(IEnumerable<(IStreamFactory, Hash, Size)> backups, CancellationToken token = default)
    {
        var builder = new NxPackerBuilder();
        var distinct = backups.DistinctBy(d => d.Item2).ToArray();
        var streams = new List<Stream>();
        foreach (var backup in distinct)
        {
            var stream = await backup.Item1.GetStreamAsync();
            streams.Add(stream);
            builder.AddFile(stream, new AddFileParams
            {
                RelativePath = backup.Item2.ToHex(),
            });
        }

        var guid = Guid.NewGuid();
        var id = guid.ToString();
        var outputPath = _archiveLocations.First().Combine(id).AppendExtension(KnownExtensions.Tmp);
        
        await using (var outputStream = outputPath.Create()){
            builder.WithOutput(outputStream);
            builder.Build();
        }
        
        foreach (var stream in streams)
            await stream.DisposeAsync();

        var finalPath = outputPath.ReplaceExtension(KnownExtensions.Nx);
        
        await outputPath.MoveToAsync(finalPath, token: token);
        await using var os = finalPath.Read();
        var unpacker = new NxUnpacker(new FromStreamProvider(os));
        UpdateIndexes(unpacker, distinct, guid, finalPath);
        

    }

    private unsafe void UpdateIndexes(NxUnpacker unpacker, (IStreamFactory, Hash, Size)[] distinct, Guid guid,
        AbsolutePath finalPath)
    {
        Span<byte> buffer = stackalloc byte[64];
        foreach (var entry in unpacker.GetFileEntriesRaw())
        {
            fixed (byte* ptr = buffer)
            {
                var writer = new LittleEndianWriter(ptr);
                entry.WriteAsV0(ref writer);
                var written = (int)((UIntPtr)writer.Ptr - (UIntPtr)ptr);
                
                var dbId = IdFor((Hash)entry.Hash, guid);

                var dbEntry = new ArchivedFiles
                {
                    File = finalPath.FileName,
                    FileEntryData = buffer.SliceFast(0, written).ToArray()
                };
                
                // TODO: Consider a bulk-put operation here
                _store.Put(dbId, dbEntry);
            }
        }
    }

    private IId IdFor(Hash hash, Guid guid)
    {
        Span<byte> buffer = stackalloc byte[24];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, hash.Value);
        guid.TryWriteBytes(buffer.SliceFast(8));
        return IId.FromSpan(EntityCategory.ArchivedFiles, buffer);
    }

    /// <inheritdoc />
    public async Task ExtractFiles(IEnumerable<(Hash Src, AbsolutePath Dest)> files, CancellationToken token = default)
    {
        var grouped = files.Distinct()
            .Select(input => TryGetLocation(input.Src, out var archivePath, out var fileEntry) 
                ? (true, Hash:input.Src, ArchivePath:archivePath, FileEntry:fileEntry, input.Dest) 
                : default)
            .Where(x => x.Item1)
            .ToLookup(l => l.ArchivePath, l => (l.Hash, l.FileEntry, l.Dest));
        
        if (grouped[default].Any())
            throw new Exception($"Missing archive for {grouped[default].First().Hash.ToHex()}");

        var settings = new UnpackerSettings();

        foreach (var group in grouped)
        {
            await using var file = group.Key.Read();
            var provider = new FromStreamProvider(file);
            var unpacker = new NxUnpacker(provider);


            var toExtract = group
                .Select(entry =>
                    (IOutputDataProvider)new OutputFileProvider(entry.Dest.Parent.GetFullPath(), entry.Dest.FileName, entry.FileEntry))
                .ToArray();

            unpacker.ExtractFiles(toExtract, settings);

            foreach (var toDispose in toExtract)
            {
                toDispose.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public Task<IDictionary<Hash, byte[]>> ExtractFiles(IEnumerable<Hash> files, CancellationToken token = default)
    {
        var results = new Dictionary<Hash, byte[]>();
        
        var grouped = files.Distinct()
            .Select(hash => TryGetLocation(hash, out var archivePath, out var fileEntry) 
                ? (true, Hash:hash, ArchivePath:archivePath, FileEntry:fileEntry) 
                : default)
            .Where(x => x.Item1)
            .ToLookup(l => l.ArchivePath, l => (l.Hash, l.FileEntry));
        
        if (grouped[default].Any())
            throw new Exception($"Missing archive for {grouped[default].First().Hash.ToHex()}");

        var settings = new UnpackerSettings();

        foreach (var group in grouped)
        {
            var byHash = group.ToLookup(f => f);
            var file = group.Key.Read();
            var provider = new FromStreamProvider(file);
            var unpacker = new NxUnpacker(provider);

            var infos = group.Select(entry => (entry.Hash, new OutputArrayProvider("", entry.FileEntry))).ToList();


            unpacker.ExtractFiles(infos.Select(o => (IOutputDataProvider)o.Item2).ToArray(), settings);

            foreach (var info in infos)
            {
                results.Add(info.Hash, info.Item2.Data);
            }
        }

        return Task.FromResult<IDictionary<Hash, byte[]>>(results);
    }

    private unsafe bool TryGetLocation(Hash hash, out AbsolutePath archivePath, out FileEntry fileEntry)
    {
        var prefix = new Id64(EntityCategory.ArchivedFiles, (ulong)hash);
        foreach (var entry in _store.GetByPrefix<ArchivedFiles>(prefix))
        {
            foreach (var location in _archiveLocations)
            {
                var path = location.Combine(entry.File);
                if (!path.FileExists) continue;

                archivePath = path;

                fixed (byte* ptr = entry.FileEntryData.AsSpan())
                {
                    var reader = new LittleEndianReader(ptr);
                    FileEntry tmpEntry = default;
                    
                    tmpEntry.FromReaderV0(ref reader);
                    fileEntry = tmpEntry;
                    return true;
                }
            }

        }

        archivePath = default;
        fileEntry = default;
        return false;
    }
}
