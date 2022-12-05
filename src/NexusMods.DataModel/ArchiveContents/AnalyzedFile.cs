using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using Wabbajack.Common.FileSignatures;

namespace NexusMods.DataModel.ArchiveContents;

[JsonName("AnalyzedFile")]
public record AnalyzedFile : Entity
{
    public required Size Size { get; init; }
    public required Hash Hash { get; init; }
    public required FileType[] FileTypes { get; init; }
    public override EntityCategory Category => EntityCategory.FileAnalysis;

    protected override Id Persist()
    { 
        var newId = new Id(Category, Hash);
        Store.Put<Entity>(newId, this);
        return newId;
    }
}