using TransparentValueObjects;

namespace NexusMods.Abstractions.GOG.Values;

/// <summary>
/// A GOG build ID.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct BuildId : IAugmentWith<JsonAugment>
{
    
}
