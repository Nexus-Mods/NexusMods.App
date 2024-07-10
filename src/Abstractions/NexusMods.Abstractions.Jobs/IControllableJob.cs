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
    ValueTask StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses the job.
    /// </summary>
    ValueTask PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the job.
    /// </summary>
    ValueTask CancelAsync(CancellationToken cancellationToken = default);
}
