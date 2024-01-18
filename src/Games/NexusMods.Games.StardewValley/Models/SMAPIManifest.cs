using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.Games.StardewValley.Models;

// ReSharper disable InconsistentNaming

/// <summary>
/// https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Toolkit/Serialization/Models/Manifest.cs#L11
/// </summary>
[PublicAPI]
[JsonName("NexusMods.Games.StardewValley.SMAPIManifest")]
public record SMAPIManifest
{
    /// <summary>
    /// The mod name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The unique mod ID.
    /// </summary>
    public required string UniqueID { get; init; }

    /// <summary>
    /// The mod version.
    /// </summary>
    public required SMAPIVersion Version { get; init; }

    /// <summary>
    /// The minimum SMAPI version required by this mod (if any).
    /// </summary>
    public SMAPIVersion? MinimumApiVersion { get; init; }

    /// <summary>
    /// The other mods that must be loaded before this mod.
    /// </summary>
    public SMAPIManifestDependency[]? Dependencies { get; init; }
}
