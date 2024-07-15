using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Represents a worker.
/// </summary>
/// <remarks>
/// Implementations can be transient or singleton.
/// </remarks>
[PublicAPI]
public interface IJobWorker
{
    /// <summary>
    /// Starts a job.
    /// </summary>
    ValueTask StartAsync(IJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a job.
    /// </summary>
    ValueTask PauseAsync(IJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a job.
    /// </summary>
    ValueTask CancelAsync(IJob job, CancellationToken cancellationToken = default);
}
