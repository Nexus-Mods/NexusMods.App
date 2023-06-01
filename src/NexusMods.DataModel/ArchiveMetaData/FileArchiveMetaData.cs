using NexusMods.Common;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Paths;

namespace NexusMods.DataModel.ArchiveMetaData;

/// <summary>
/// Archive Meta data for a file archive, where it cam
/// </summary>
[JsonName("NexusMods.DataMode.ArchiveMetaData")]
public record FileArchiveMetaData : AArchiveMetaData
{
    /// <summary>
    /// The filename of the file
    /// </summary>
    public required RelativePath OriginalPath { get; init; }

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
            Quality = Quality.Low,
            OriginalPath = path.FileName,
            Name = path.GetFileNameWithoutExtension(),
            Size = archive.Size,
            Hash = archive.Hash
        };
    }
}
