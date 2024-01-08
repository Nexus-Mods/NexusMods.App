using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.ArchiveMetaData;


/// <summary>
/// Archive metadata for a download that was installed from an existing game archive.
/// </summary>
[JsonName("NexusMods.DataModel.ArchiveMetaData.GameArchiveMetadata")]
public record GameArchiveMetadata : AArchiveMetaData
{
    public required GameInstallation Installation { get; init; }
}
