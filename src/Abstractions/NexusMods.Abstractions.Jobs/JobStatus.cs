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
