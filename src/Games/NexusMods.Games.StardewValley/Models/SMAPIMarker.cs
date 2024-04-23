using System.Diagnostics.CodeAnalysis;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization.Attributes;
using StardewModdingAPI;
using StardewModdingAPI.Toolkit;

namespace NexusMods.Games.StardewValley.Models;

[JsonName("NexusMods.Games.StardewValley.Models.SMAPIMarker")]
public record SMAPIMarker : AModMetadata
{
    public required string Version { get; init; }

    public bool TryParse([NotNullWhen(true)] out ISemanticVersion? semanticVersion)
    {
        return SemanticVersion.TryParse(Version, out semanticVersion);
    }
}
