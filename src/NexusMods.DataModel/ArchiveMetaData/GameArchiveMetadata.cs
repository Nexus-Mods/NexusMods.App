using NexusMods.DataModel.Games;

namespace NexusMods.DataModel.ArchiveMetaData;

public record GameArchiveMetadata : AArchiveMetaData
{
    public required GameInstallation Installation { get; init; }
}
