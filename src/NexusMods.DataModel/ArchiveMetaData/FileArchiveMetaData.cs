using NexusMods.Common;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.Paths;

namespace NexusMods.DataModel.ArchiveMetaData;

/// <summary>
/// Archive Meta data for a file archive, where it cam
/// </summary>
public record FileArchiveMetaData : AArchiveMetaData
{
    /// <summary>
    /// The full path to the file on disk, when this metadata was created.
    /// </summary>
    public required AbsolutePath OriginalPath { get; init; }

    /// <summary>
    /// Create a new FileArchiveMetaData object from an AnalyzedArchive and a raw path
    /// </summary>
    /// <param name="path"></param>
    /// <param name="archive"></param>
    /// <returns></returns>
    public static FileArchiveMetaData Create(AbsolutePath path, AnalyzedArchive archive)
    {
        return new FileArchiveMetaData
        {
            Priority = Priority.Low,
            OriginalPath = path,
            Name = path.GetFileNameWithoutExtension(),
            Size = archive.Size,
            Hash = archive.Hash
        };
    }
}
