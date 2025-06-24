using JetBrains.Annotations;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

/// <summary>
/// Status of a collection.
/// </summary>
[PublicAPI]
public enum CollectionStatus
{
    /// <summary>
    /// The collection is unlisted, and only the author or anyone with a direct link can view it.
    /// </summary>
    Unlisted = 0,

    /// <summary>
    /// The collection is publicly available.
    /// </summary>
    Listed = 1,
}
