using System.Buffers.Binary;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.DataModel;

public class ArchiveManagerEx : IArchiveManager
{
    private readonly AbsolutePath[] _archiveLocations;
    private readonly IDataStore _store;

    public ArchiveManagerEx(IDataStore store, IDataModelSettings settings)
    {
        _archiveLocations = settings.ArchiveLocations.Select(f => f.ToAbsolutePath()).ToArray();
        foreach (var location in _archiveLocations)
        {
            if (!location.DirectoryExists())
                location.CreateDirectory();
        }
        _store = store;

    }
    
    public async ValueTask<bool> HaveFile(Hash hash)
    {
        return Location(hash) != null;
    }

    public async Task BackupFiles(IEnumerable<(IStreamFactory, Hash, Size)> backups, CancellationToken token = default)
    {
        var builder = new NxPackerBuilder();
        foreach (var backup in backups)
        {
            builder.AddFile(await backup.Item1.GetStreamAsync(), new AddFileParams
            {
                RelativePath = backup.Item2.ToHex(),
            });
        }

        var guid = Guid.NewGuid();
        var id = guid.ToString();
        var outputPath = _archiveLocations.First().CombineUnchecked(id).AppendExtension(KnownExtensions.Tmp);
        
        await using (var outputStream = outputPath.Create()){
            builder.WithOutput(outputStream);
            builder.Build();
        }

        var finalPath = outputPath.ReplaceExtension(KnownExtensions.Nx);
        await outputPath.MoveToAsync(finalPath, token: token);
        
        foreach (var file in backups)
        {
            var dbId = IdFor(file.Item2, guid);
            var entry = new FileContainedInEx
            {
                File = finalPath.FileName
            };
            _store.Put(dbId, entry);
        }
    }

    private IId IdFor(Hash hash, Guid guid)
    {
        Span<byte> buffer = stackalloc byte[24];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, hash.Value);
        guid.TryWriteBytes(buffer.SliceFast(8));
        return IId.FromSpan(EntityCategory.FileContainedInEx, buffer);
    }

    public async Task ExtractFiles(IEnumerable<(Hash Src, AbsolutePath Dest)> files, CancellationToken token = default)
    {
        var grouped = files.ToLookup(l => Location(l.Src));
        if (grouped[default].Any())
            throw new Exception($"Missing archive for {grouped[default].First().Src.ToHex()}");

        var settings = new UnpackerSettings();

        foreach (var group in grouped)
        {
            var byHash = group.ToLookup(f => f.Src);
            var file = group.Key.Read();
            var provider = new FromStreamProvider(file);
            var unpacker = new NxUnpacker(provider);

            var infos = new List<IOutputDataProvider>();
            
            
            foreach (var entry in unpacker.GetFileEntriesRaw().ToArray())
            {
                var entryHash = Hash.From(entry.Hash);
                if (!byHash[entryHash].Any()) continue;

                infos.AddRange(byHash[entryHash].Select(dest => 
                    new OutputFileProvider(dest.Dest.Parent.GetFullPath(), dest.Dest.FileName, entry)));
            }
            
            unpacker.ExtractFiles(infos.ToArray(), settings);
        }
    }

    public async Task<IDictionary<Hash, byte[]>> ExtractFiles(IEnumerable<Hash> files, CancellationToken token = default)
    {
        var results = new Dictionary<Hash, byte[]>();
        
        var grouped = files.Distinct().ToLookup(Location);
        if (grouped[default].Any())
            throw new Exception($"Missing archive for {grouped[default].First().ToHex()}");

        var settings = new UnpackerSettings();

        foreach (var group in grouped)
        {
            var byHash = group.ToLookup(f => f);
            var file = group.Key.Read();
            var provider = new FromStreamProvider(file);
            var unpacker = new NxUnpacker(provider);

            var infos = new List<(Hash Hash, OutputArrayProvider Data)>();
            
            
            foreach (var entry in unpacker.GetFileEntriesRaw().ToArray())
            {
                var entryHash = Hash.From(entry.Hash);
                if (!byHash[entryHash].Any()) continue;

                infos.AddRange(byHash[entryHash].Select(hash => (hash, new OutputArrayProvider("", entry))));
            }
            
            unpacker.ExtractFiles(infos.Select(o => (IOutputDataProvider)o.Item2).ToArray(), settings);

            foreach (var info in infos)
            {
                results.Add(info.Hash, info.Data.Data);
            }
        }

        return results;
    }

    public AbsolutePath Location(Hash hash)
    {
        var prefix = new Id64(EntityCategory.FileContainedInEx, (ulong)hash);
        return _store.GetByPrefix<FileContainedInEx>(prefix)
            .SelectMany(f =>
            {
                return _archiveLocations.Select(loc => loc.CombineUnchecked(f.File));
            })
            .FirstOrDefault(path => path.FileExists);
        
    }
}
