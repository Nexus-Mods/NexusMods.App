using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.ArchiveMetaData;

/// <summary>
/// Info for a mod archive describing where it came from and the suggested name of the file
/// </summary>
public abstract record AArchiveMetaData : Entity
{
    /// <summary>
    /// A human readable name for the archive.
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// The size of the archive.
    /// </summary>
    public required Size Size { get; init; }
    
    /// <summary>
    /// The hash of the archive.
    /// </summary>
    public required Hash Hash { get; init; }
    
    /// <summary>
    /// How accurate is this metadata? Data from a file on disk is more generic than data
    /// from a NexusMods API call.
    /// </summary>
    public required Quality Quality { get; init; }
    
    public override EntityCategory Category => EntityCategory.ArchiveMetaData;

    protected override IId Persist(IDataStore store)
    {
        var id = store.ContentHashId(this, out var data);
        var newId = new TwoId64(Category, Hash.Value, id.ToUInt64());
        store.PutRaw(newId, data);
        return newId;
    }

    /// <summary>
    /// Get all the meta data for the given archive. Data is returned in order of priority.
    /// </summary>
    /// <param name="store"></param>
    /// <param name="archiveHash"></param>
    /// <returns></returns>
    public static IEnumerable<AArchiveMetaData> GetMetaDatas(IDataStore store, Hash archiveHash)
    {
        return store.GetByPrefix<AArchiveMetaData>(new Id64(EntityCategory.ArchiveMetaData, archiveHash.Value))
            .OrderBy(x => x.Quality);
    }
}
