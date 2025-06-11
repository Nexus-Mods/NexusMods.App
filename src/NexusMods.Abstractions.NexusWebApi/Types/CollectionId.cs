using TransparentValueObjects;

namespace NexusMods.Abstractions.NexusWebApi.Types;

/// <summary>
/// Globally unique identifier of a collection.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct CollectionId { }
