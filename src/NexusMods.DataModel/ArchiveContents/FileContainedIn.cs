using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.ArchiveContents;

[JsonName("FileContainedIn")]
public record FileContainedIn : Entity
{
    public required Hash File { get; init; }
    public required Hash Parent { get; init; }
    public required RelativePath Path { get; init; }

    /// <summary>
    /// Stores the entity in the data store, using a <see cref="TwoId64"/> as the ID.
    /// Calculated based on the <see cref="Category"/>, <see cref="File"/> and <see cref="Parent"/> fields.
    /// </summary>
    /// <param name="store"></param>
    /// <returns></returns>
    protected override IId Persist(IDataStore store)
    {
        var id = new TwoId64(Category, (ulong)File, (ulong)Parent);
        store.Put(id, this);
        return id;
    }

    public override EntityCategory Category => EntityCategory.FileContainedIn;
}
