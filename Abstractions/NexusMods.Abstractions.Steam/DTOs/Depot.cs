using NexusMods.Abstractions.Steam.Values;

namespace NexusMods.Abstractions.Steam.DTOs;

/// <summary>
/// Information about a depot on Steam.
/// </summary>
public class Depot
{
    /// <summary>
    /// The OSes that the depot is available on.
    /// </summary>
    public required string[] OsList { get; init; }
    
    /// <summary>
    /// The id of the depot.
    /// </summary>
    public required DepotId DepotId { get; init; }
    
    /// <summary>
    /// The manifests associated with the depot, with a key for each available branch
    /// </summary>
    public required Dictionary<string, ManifestInfo> Manifests { get; init; }
}
