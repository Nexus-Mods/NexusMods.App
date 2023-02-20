using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.Games.MountAndBladeBannerlord.Loadouts;

[JsonName("MountAndBladeBannerlord.Loadouts.ModuleIdMetadata")]
public class ModuleIdMetadata : IModFileMetadata
{
    public required string ModuleId { get; init; } = string.Empty;
}