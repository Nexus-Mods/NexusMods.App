using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Loadouts;

[JsonName("MountAndBlade2Bannerlord.Loadouts.ModuleIdMetadata")]
public class ModuleIdMetadata : IModFileMetadata
{
    public required string ModuleId { get; init; } = string.Empty;
}
