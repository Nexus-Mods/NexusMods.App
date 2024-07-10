using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Represents a worker.
/// </summary>
[PublicAPI]
public interface IJobWorker
{
    /// <summary>
    /// Gets the job
    /// </summary>
    IJob Job { get; }

    /// <summary>
    /// Starts a job.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a job.
    /// </summary>
    Task PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a job.
    /// </summary>
    Task CancelAsync(CancellationToken cancellationToken = default);
}
