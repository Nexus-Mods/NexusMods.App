using TransparentValueObjects;

namespace NexusMods.DataModel.Interprocess.Jobs;

/// <summary>
/// Cross platform wrapper for a process id.
/// </summary>
[ValueObject<uint>]
public readonly partial struct ProcessId { }
