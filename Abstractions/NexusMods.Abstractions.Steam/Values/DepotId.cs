using TransparentValueObjects;

namespace NexusMods.Abstractions.Steam.Values;

/// <summary>
/// A globally unique identifier for a depot, a reference to a collection of files on the Steam CDN.
/// </summary>
[ValueObject<uint>]
public readonly partial struct DepotId : IAugmentWith<JsonAugment>
{
    
}
