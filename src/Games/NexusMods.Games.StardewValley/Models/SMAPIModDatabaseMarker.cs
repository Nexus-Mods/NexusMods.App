using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.Games.StardewValley.Models;

[JsonName("NexusMods.Games.StardewValley.Models.SMAPIModDatabaseMarker")]
public record SMAPIModDatabaseMarker : IMetadata;
