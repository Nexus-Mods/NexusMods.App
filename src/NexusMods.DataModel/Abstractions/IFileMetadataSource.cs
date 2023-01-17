using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Loadouts;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Interfaces.Components;
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
    public IEnumerable<string> Games { get; }

    /// <summary>
    /// Return metadata for the given file. If metadata for this source already exists on the file
    /// it will be removed before the new metadata is added, so this method should always return
    /// all metadata for the given file.
    /// </summary>
    /// <param name="filLoadout"></param>
    /// <param name="mod"></param>
    /// <param name="file"></param>
    /// <param name="analyzedFile"></param>
    /// <returns></returns>
    public IAsyncEnumerable<IModFileMetadata> GetMetadata(Loadout filLoadout, Mod mod, AModFile file,
        AnalyzedFile analyzedFile);
}