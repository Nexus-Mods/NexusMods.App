using TransparentValueObjects;

namespace NexusMods.DataModel.Interprocess.Jobs;

/// <summary>
/// A unique identifier for a job.
/// </summary>
[ValueObject<Guid>]
public readonly partial struct JobId { }
