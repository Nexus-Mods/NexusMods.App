using TransparentValueObjects;

namespace NexusMods.Abstractions.NexusWebApi.Types;

/// <summary>
/// revision number (aka "version") of a revision. Only unique within one collection
/// </summary>
[ValueObject<ulong>]
public readonly partial struct RevisionNumber { }
