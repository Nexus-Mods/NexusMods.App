namespace NexusMods.Abstractions.Activities;

/// <summary>
/// The status of an activity.
/// </summary>
public enum ActivityStatus
{
    /// <summary>
    /// Activity is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// The activity has finished.
    /// </summary>
    Finished,

    /// <summary>
    /// The activity has failed.
    /// </summary>
    Failed,

    /// <summary>
    /// The activity has been cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The activity has been paused.
    /// </summary>
    Paused
}
