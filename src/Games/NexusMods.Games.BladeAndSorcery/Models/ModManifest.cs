using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.Games.BladeAndSorcery.Models;

[JsonName("NexusMods.Games.BladeAndSorcery.ModManifest")]
public record ModManifest(
    string Name,
    string Description,
    string Author,
    string ModVersion,
    string GameVersion,
    string Thumbnail);
