using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

[PublicAPI]
public enum JobResultType : byte
{
    /// <summary>
    /// The job finished successfully.
    /// </summary>
    Completed = 0,

    /// <summary>
    /// The job was cancelled.
    /// </summary>
    Cancelled = 1,

    /// <summary>
    /// The job failed.
    /// </summary>
    Failed = 2,
}
