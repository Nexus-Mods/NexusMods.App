using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Paths;

namespace NexusMods.DataModel.ArchiveContents;

/// <summary>
/// Represents an individual archive which has been scanned by an implementation
/// of <see cref="IFileAnalyzer"/>.
/// </summary>
[JsonName("AnalyzedArchive")]
public record AnalyzedArchive : AnalyzedFile
{
    /// <summary>
    /// A mapping of relative paths inside the archive to respective files
    /// contained inside.
    /// </summary>
    public required EntityDictionary<RelativePath, AnalyzedFile> Contents { get; init; }
}
