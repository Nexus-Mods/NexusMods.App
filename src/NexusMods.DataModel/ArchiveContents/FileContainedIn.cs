using NexusMods.DataModel.Abstractions;
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

    protected override Id Persist()
    {
        var id = new TwoId64(Category, File, Parent);
        Store.Put(id, this);
        return id;
    }

    public override EntityCategory Category => EntityCategory.FileContainedIn;
}