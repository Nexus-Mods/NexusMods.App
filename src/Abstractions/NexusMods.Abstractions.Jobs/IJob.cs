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
    /// Gets the parent job group.
    /// </summary>
    IJobGroup? Group { get; }

    /// <summary>
    /// Gets the status of the job.
    /// </summary>
    JobStatus Status { get; }

    /// <summary>
    /// Gets the progress of the job.
    /// </summary>
    Progress Progress { get; }

    /// <summary>
    /// Returns a proxy task that completes when the job is finished.
    /// </summary>
    /// <param name="cancellationToken">
    /// Optional cancellation token to stop the waiting. Note that this only
    /// cancels the proxy task, it does not cancel the job.
    /// </param>
    Task<JobResult> WaitToFinishAsync(CancellationToken cancellationToken = default);
}
