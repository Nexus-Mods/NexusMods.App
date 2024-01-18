using TransparentValueObjects;

namespace NexusMods.DataModel;

/// <summary>
/// A unique identifier for an archive in a ArchiveManager
/// </summary>
[ValueObject<Guid>]
public readonly partial struct ArchiveId { }
