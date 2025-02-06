using DynamicData.Kernel;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Represents the status of a job.
/// </summary>
[PublicAPI]
public enum JobStatus : byte
{
    /// <summary>
    /// Default value.
    /// </summary>
    None = 0,

    /// <summary>
    /// The job has been created and initialized.
    /// </summary>
    /// <remarks>
    /// Jobs in this status can transition to <see cref="Running"/>.
    /// </remarks>
    Created = 1,

    /// <summary>
    /// The job is being executed.
    /// </summary>
    /// <remarks>
    /// Jobs in this status can transition to <see cref="Paused"/>,
    /// <see cref="Completed"/>, <see cref="Cancelled"/>, and <see cref="Failed"/>.
    /// </remarks>
    Running = 2,

    /// <summary>
    /// The job is paused.
    /// </summary>
    /// <remarks>
    /// Jobs in this status can transition to <see cref="Running"/> or
    /// <see cref="Cancelled"/>.
    /// </remarks>
    Paused = 3,

    /// <summary>
    /// The job completed successfully.
    /// </summary>
    /// <remarks>
    /// Jobs in this status are considered finished and can't transition to a
    /// different status.
    /// </remarks>
    Completed = 4,

    /// <summary>
    /// The job was cancelled by the user.
    /// </summary>
    /// <remarks>
    /// Jobs in this status are considered finished and can't transition to a
    /// different status.
    /// </remarks>
    Cancelled = 5,

    /// <summary>
    /// The job failed
    /// </summary>
    /// <remarks>
    /// Jobs in this status are considered finished and can't transition to a
    /// different status.
    /// </remarks>
    Failed = 6,
}

/// <summary>
/// Comparer.
/// </summary>
[PublicAPI]
public class JobStatusComparer : IComparer<JobStatus>
{
    /// <summary>
    /// Instance.
    /// </summary>
    public static readonly IComparer<JobStatus> Instance = new JobStatusComparer();

    /// <inheritdoc/>
    public int Compare(JobStatus x, JobStatus y)
    {
        var a = (byte)x;
        var b = (byte)y;
        return a.CompareTo(b);
    }
}

/// <summary>
/// Extensions for the <see cref="JobStatus"/> type.
/// </summary>
public static class JobStatusExtensions
{
    /// <summary>
    /// Determines if a job is "active" based on the current job status.
    /// A job is considered "active" if the status is <see cref="JobStatus.Running"/> or <see cref="JobStatus.Paused"/>.
    /// </summary>
    /// <param name="currentStatus">The current job status.</param>
    /// <returns>
    /// <c>true</c> if the job is active, <c>false</c> otherwise.
    /// </returns>
    public static bool IsActive(this JobStatus currentStatus) => currentStatus is JobStatus.Running or JobStatus.Paused;

    /// <summary>
    /// Determines if a job was "activated" based on the previous and current job status.
    /// A job is considered "activated" if the status changed to <see cref="JobStatus.Running"/> or <see cref="JobStatus.Paused"/> from any other state.
    /// </summary>
    /// <param name="currentStatus">The current job status.</param>
    /// <param name="previousStatus">The previous job status, or <see langword="null"/> if the job is being created.</param>
    /// <returns>
    /// <c>true</c> if the job was activated, <c>false</c> otherwise.
    /// </returns>
    public static bool WasActivated(this JobStatus currentStatus, Optional<JobStatus> previousStatus)
    {
        // We set to 'none' because the activation queue is setting to `Running` or `Paused`.
        return WasActivated(currentStatus, !previousStatus.HasValue ? JobStatus.None : previousStatus.Value);
    }
    
    /// <summary>
    /// Determines if a job was "deactivated" based on the previous and current job status.
    /// A job is considered "deactivated" if the status changed from <see cref="JobStatus.Running"/> or <see cref="JobStatus.Paused"/> to any other state.
    /// </summary>
    /// <param name="currentStatus">The current job status.</param>
    /// <param name="previousStatus">The previous job status, or <see langword="null"/> if the job is being created.</param>
    /// <returns>
    /// <c>true</c> if the job was deactivated, <c>false</c> otherwise.
    /// </returns>
    public static bool WasDeactivated(this JobStatus currentStatus, Optional<JobStatus> previousStatus)
    {
        // We set to 'running' on null because the deactivation queue is setting away from `Running` or `Paused`.
        return WasDeactivated(currentStatus, !previousStatus.HasValue ? JobStatus.Running : previousStatus.Value);
    }
    
    /// <summary>
    /// Determines if a job was "activated" based on the previous and current job status.
    /// A job is considered "activated" if the status changed to <see cref="JobStatus.Running"/> or <see cref="JobStatus.Paused"/> from any other state.
    /// </summary>
    /// <param name="currentStatus">The current job status.</param>
    /// <param name="previousStatus">The previous job status, or <see langword="null"/> if the job is being created.</param>
    /// <returns>
    /// <c>true</c> if the job was activated, <c>false</c> otherwise.
    /// </returns>
    public static bool WasActivated(this JobStatus currentStatus, JobStatus previousStatus)
    {
        var isCurrentStatusActivated = currentStatus is JobStatus.Running or JobStatus.Paused;
        var wasPreviousStatusActivated = previousStatus is JobStatus.Running or JobStatus.Paused;
        return isCurrentStatusActivated && !wasPreviousStatusActivated;
    }
    
    /// <summary>
    /// Determines if a job was "deactivated" based on the previous and current job status.
    /// A job is considered "deactivated" if the status changed from <see cref="JobStatus.Running"/> or <see cref="JobStatus.Paused"/> to any other state.
    /// </summary>
    /// <param name="currentStatus">The current job status.</param>
    /// <param name="previousStatus">The previous job status, or <see langword="null"/> if the job is being created.</param>
    /// <returns>
    /// <c>true</c> if the job was deactivated, <c>false</c> otherwise.
    /// </returns>
    public static bool WasDeactivated(this JobStatus currentStatus, JobStatus previousStatus)
    {
        var wasPreviousStatusActivated = previousStatus is JobStatus.Running or JobStatus.Paused;
        var isCurrentStatusActivated = currentStatus is JobStatus.Running or JobStatus.Paused;
        return wasPreviousStatusActivated && !isCurrentStatusActivated;
    }
}
