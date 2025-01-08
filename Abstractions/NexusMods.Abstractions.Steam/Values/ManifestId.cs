using TransparentValueObjects;

namespace NexusMods.Abstractions.Steam.Values;

/// <summary>
/// A global unique identifier for a manifest, a specific collection of files that can be downloaded
/// </summary>
[ValueObject<ulong>]
public readonly partial struct ManifestId : IAugmentWith<JsonAugment>
{
    
}
