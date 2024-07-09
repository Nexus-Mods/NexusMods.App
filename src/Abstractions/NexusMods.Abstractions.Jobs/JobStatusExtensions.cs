using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Extension methods for <see cref="JobStatus"/>.
/// </summary>
[PublicAPI]
public static class JobStatusExtensions
{
    /// <summary>
    /// Returns whether the status is considered "finished" meaning either
    /// <see cref="JobStatus.Completed"/>, <see cref="JobStatus.Cancelled"/>,
    /// or <see cref="JobStatus.Failed"/>.
    /// </summary>
    public static bool IsFinished(this JobStatus status)
    {
        return status switch
        {
            JobStatus.Completed => true,
            JobStatus.Cancelled => true,
            JobStatus.Failed => true,
            _ => false,
        };
    }

    /// <summary>
    /// Returns whether a transition from one status to another is valid.
    /// </summary>
    public static bool CanTransition(this JobStatus from, JobStatus to)
    {
        return (from, to) switch
        {
            (JobStatus.None, _ ) => true,

            (JobStatus.Created, JobStatus.Running) => true,
            (JobStatus.Created, _) => false,

            (JobStatus.Running, JobStatus.Paused) => true,
            (JobStatus.Running, JobStatus.Completed) => true,
            (JobStatus.Running, JobStatus.Cancelled) => true,
            (JobStatus.Running, JobStatus.Failed) => true,
            (JobStatus.Running, _) => false,

            (JobStatus.Paused, JobStatus.Running) => true,
            (JobStatus.Paused, JobStatus.Cancelled) => true,
            (JobStatus.Paused, _) => false,

            (JobStatus.Completed, _) => false,
            (JobStatus.Cancelled, _) => false,
            (JobStatus.Failed, _) => false,
        };
    }
}
