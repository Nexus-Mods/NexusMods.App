using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Represents a piece of work.
/// </summary>
[PublicAPI]
public interface IJob
{
    /// <summary>
    /// Gets the ID of the job.
    /// </summary>
    JobId Id { get; }

    /// <summary>
    /// Gets the parent job.
    /// </summary>
    IJob? ParentJob { get; }

    /// <summary>
    /// Gets the status of the job.
    /// </summary>
    JobStatus Status { get; }

    /// <summary>
    /// Gets the progress of the job.
    /// </summary>
    IProgress Progress { get; }
}
