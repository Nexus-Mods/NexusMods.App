using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Represents a controllable job.
/// </summary>
[PublicAPI]
public interface IControllableJob : IJob
{
    /// <summary>
    /// Starts the job.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses the job.
    /// </summary>
    Task PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the job.
    /// </summary>
    Task CancelAsync(CancellationToken cancellationToken = default);
}
