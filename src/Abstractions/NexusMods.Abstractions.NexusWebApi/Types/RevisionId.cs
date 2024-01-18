using TransparentValueObjects;

namespace NexusMods.Abstractions.NexusWebApi.Types;

/// <summary>
/// globally unique id identifying a specific revision of a collection
/// </summary>
[ValueObject<ulong>]
public readonly partial struct RevisionId { }
