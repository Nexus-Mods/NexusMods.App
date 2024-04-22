using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.Games.StardewValley.Models;

[JsonName("NexusMods.Games.StardewValley.Models.SMAPIMarker")]
public record SMAPIMarker : AModMetadata
{
    public required string Version { get; init; }
}
