using System.Buffers.Binary;
using NexusMods.Archives.Nx.Packing;
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

    public Task ExtractFiles(IEnumerable<(Hash Src, IStreamFactory Dest)> files, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public AbsolutePath? Location(Hash hash)
    {
        var prefix = new Id64(EntityCategory.FileContainedIn, (ulong)hash);
        return _store.GetByPrefix<FileContainedInEx>(prefix)
            .SelectMany(f =>
            {
                return _archiveLocations.Select(loc => loc.CombineUnchecked(f.File));
            })
            .FirstOrDefault(path => path.FileExists);
        
    }
}
