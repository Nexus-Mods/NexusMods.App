using System.Collections.Immutable;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.JsonConverters;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.ArchiveContents;

[JsonName("AnalyzedFile")]
public record AnalyzedFile : Entity
{
    public required Size Size { get; init; }
    public required Hash Hash { get; init; }
    public required FileType[] FileTypes { get; init; }
    public ImmutableList<IFileAnalysisData> AnalysisData { get; init; } = ImmutableList<IFileAnalysisData>.Empty;

    public override EntityCategory Category => EntityCategory.FileAnalysis;

    protected override IId Persist()
    {
        var newId = new Id64(Category, (ulong)Hash);
        Store.Put<Entity>(newId, this);
        return newId;
    }
}
