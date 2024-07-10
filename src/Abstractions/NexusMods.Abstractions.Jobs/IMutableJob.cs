using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Mutable variant of <see cref="IJob"/>.
/// </summary>
[PublicAPI]
public interface IMutableJob : IControllableJob
{
    /// <summary>
    /// Gets the mutable progress of the job.
    /// </summary>
    new MutableProgress Progress { get; }

    /// <summary>
    /// Setter for <see cref="IJob.Status"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the transition is invalid. Use <see cref="JobStatusExtensions.CanTransition"/>
    /// before changing the status.
    /// </exception>
    void SetStatus(JobStatus value);
}
