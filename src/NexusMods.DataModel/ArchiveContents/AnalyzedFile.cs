using System.Collections.Immutable;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.JsonConverters;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.ArchiveContents;

/// <summary>
/// Represents an individual file which has been scanned by an implementation
/// of <see cref="IFileAnalyzer"/>.<br/><br/>
///
/// This file doesn't have to represent a file on disk, it can also represent
/// a file stored inside an archive.
/// </summary>
[JsonName("AnalyzedFile")]
public record AnalyzedFile : Entity
{
    /// <summary>
    /// Size of the file in bytes.
    /// </summary>
    public required Size Size { get; init; }

    /// <summary>
    /// Individual hash of the file.
    /// </summary>
    public required Hash Hash { get; init; }

    /// <summary>
    /// Hash of the ids of the analyzers used to analyze this file.
    /// </summary>
    public required Hash AnalyzersHash { get; init; }

    /// <summary>
    /// Stores the types of file this file is classified by.
    /// </summary>
    /// <remarks>
    /// This field is based on signatures/magic values stored in file headers.
    /// Usually there will only be one result here, but it is not impossible
    /// for there to be multiple matches.
    /// </remarks>
    public required FileType[] FileTypes { get; init; }

    /// <summary>
    /// Stores any data returned from a <see cref="IFileAnalyzer"/> that might be
    /// useful later.
    /// </summary>
    /// <remarks>
    ///    This information is <see cref="IFileAnalyzer"/> specific and must be
    ///    casted using e.g. `IFileAnalysisData as PluginData data` before use.
    /// </remarks>
    public ImmutableList<IFileAnalysisData> AnalysisData { get; init; } = ImmutableList<IFileAnalysisData>.Empty;

    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.FileAnalysis;

    /// <summary>
    /// Persist the entity in the data store. Calculates the ID based on the Hash field.
    /// </summary>
    /// <param name="store"></param>
    /// <returns></returns>
    protected override IId Persist(IDataStore store)
    {
        var newId = new Id64(Category, (ulong)Hash);
        store.Put<Entity>(newId, this);
        return newId;
    }
}
