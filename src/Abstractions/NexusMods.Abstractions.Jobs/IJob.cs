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
    /// Gets the worker of this job.
    /// </summary>
    /// <remarks>
    /// This value may not be unavailable if the job isn't ready to be run yet,
    /// or if the job finished.
    /// </remarks>
    IJobWorker? Worker { get; }

    /// <summary>
    /// Gets the status of the job.
    /// </summary>
    JobStatus Status { get; }

    /// <summary>
    /// Gets the progress of the job.
    /// </summary>
    Progress Progress { get; }

    /// <summary>
    /// Gets the observable stream for changes to <see cref="Status"/>.
    /// </summary>
    IObservable<JobStatus> ObservableStatus { get; }

    /// <summary>
    /// Returns a proxy task that completes when the job is finished.
    /// </summary>
    /// <param name="cancellationToken">
    /// Optional cancellation token to stop the waiting. Note that this only
    /// cancels the proxy task, it does not cancel the job.
    /// </param>
    Task<JobResult> WaitToFinishAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the job.
    /// </summary>
    /// <returns>
    /// A task that completes when the job has been started, not when the job
    /// has completed.
    /// </returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses the job.
    /// </summary>
    /// <remarks>
    /// A task that completes when the job has been paused.
    /// </remarks>
    Task PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the job.
    /// </summary>
    /// <returns>
    /// A task that completes when the job has been cancelled.
    /// </returns>
    Task CancelAsync(CancellationToken cancellationToken = default);
}
