using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Loadouts;

[JsonName("MountAndBlade2Bannerlord.Loadouts.OriginalPathMetadata")]
public class OriginalPathMetadata : IModFileMetadata
{
    public required string OriginalRelativePath { get; init; } = string.Empty;
}
