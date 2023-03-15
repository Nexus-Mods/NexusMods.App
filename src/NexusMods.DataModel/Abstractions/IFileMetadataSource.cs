using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;

namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// Defines a source of file metadata, will be called whenever new files
/// with the given filters are added to a loadout, or when they are modified.
/// </summary>
public interface IFileMetadataSource
{
    /// <summary>
    /// Apply this source to files with the given extensions
    /// </summary>
    public IEnumerable<Extension> Extensions { get; }

    /// <summary>
    /// Apply this source to the given file types
    /// </summary>
    public IEnumerable<FileType> FileTypes { get; }

    /// <summary>
    /// Apply this source to the given games
    /// </summary>
    public IEnumerable<GameDomain> Games { get; }

    /// <summary>
    /// Return metadata for the given file.
    /// </summary>
    /// <param name="loadout">The mod loadout for which the metadata is to be fetched.</param>
    /// <param name="mod">The mod which owns the <paramref name="file"/> and <paramref name="analyzedFile"/>,</param>
    /// <param name="file">
    ///     Individual file belonging to the mod, the
    ///     <paramref name="analyzedFile"/> corresponds to this file.
    /// </param>
    /// <param name="analyzedFile">Individual file returned as a result of analysis.</param>
    /// <returns></returns>
    /// <remarks>
    ///     During file analysis, if metadata for this source already exists on the file
    ///     it will be removed before the new metadata is added, so this method should always return
    ///     all metadata for the given file.
    /// </remarks>
    public IAsyncEnumerable<IModFileMetadata> GetMetadataAsync(Loadout loadout, Mod mod, AModFile file,
        AnalyzedFile analyzedFile);
}
