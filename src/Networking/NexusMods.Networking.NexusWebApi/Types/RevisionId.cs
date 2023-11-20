using TransparentValueObjects;

namespace NexusMods.Networking.NexusWebApi.Types;

/// <summary>
/// globally unique id identifying a specific revision of a collection
/// </summary>
[ValueObject<ulong>]
public readonly partial struct RevisionId { }
