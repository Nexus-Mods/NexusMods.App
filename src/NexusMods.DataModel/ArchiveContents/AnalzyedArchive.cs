using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Paths;

namespace NexusMods.DataModel.ArchiveContents;

[JsonName("AnalyzedArchive")]
public record AnalyzedArchive : AnalyzedFile
{
    public required EntityDictionary<RelativePath, AnalyzedFile> Contents { get; init; }
}
