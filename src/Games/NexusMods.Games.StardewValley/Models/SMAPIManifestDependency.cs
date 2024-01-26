using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.Games.StardewValley.Models;

// ReSharper disable InconsistentNaming

/// <summary>
/// https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Toolkit.CoreInterfaces/IManifestDependency.cs
/// https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Toolkit/Serialization/Models/ManifestDependency.cs
/// </summary>
[PublicAPI]
[JsonName("NexusMods.Games.StardewValley.Models.SMAPIManifestDependency")]
public record SMAPIManifestDependency
{
    /// <summary>
    /// The unique mod ID to require.
    /// </summary>
    public required string UniqueID { get; init; }

    /// <summary>
    /// The minimum required version. This property is optional.
    /// </summary>
    public SMAPIVersion? MinimumVersion { get; init; }

    /// <summary>
    /// Whether the dependency must be installed to use the mod. This property is
    /// optional and set to <c>true</c> by default (https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Toolkit/Serialization/Models/ManifestDependency.cs#L29)
    /// </summary>
    public bool IsRequired { get; init; } = true;
}
