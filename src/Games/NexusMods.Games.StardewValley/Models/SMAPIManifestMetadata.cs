using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.Games.StardewValley.Models;

/// <summary>
/// Marker for manifest files.
/// </summary>
[PublicAPI]
[JsonName("NexusMods.Games.StardewValley.Models.SMAPIManifestMetadata")]
public class SMAPIManifestMetadata : IMetadata;
