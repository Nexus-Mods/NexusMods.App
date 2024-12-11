using NexusMods.Abstractions.Steam.Values;

namespace NexusMods.Abstractions.Steam.DTOs;

/// <summary>
/// Information about a depot on Steam.
/// </summary>
public class Depot
{
    /// <summary>
    /// The name of the depot.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// The app id associated with the depot.
    /// </summary>
    public required AppId AppId { get; init; }
    
    /// <summary>
    /// The id of the depot.
    /// </summary>
    public required DepotId DepotId { get; init; }
    
    /// <summary>
    /// The Current ManifestId of the depot.
    /// </summary>
    public required ManifestId CurrentManifestId { get; init; }
}
