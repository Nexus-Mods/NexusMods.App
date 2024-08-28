namespace NexusMods.Abstractions.DurableJobs;

/// <summary>
/// The possible states of a job.
/// </summary>
public enum JobState
{
    /// <summary>
    /// The job has been created but has not yet started.
    /// </summary>
    Created,
    /// <summary>
    /// The job is actively running.
    /// </summary>
    Running,
    /// <summary>
    /// The job is waiting for some sub-job to complete
    /// </summary>
    Waiting,
    
    /// <summary>
    /// The job has failed.
    /// </summary>
    Failed,
    
    /// <summary>
    /// The job has completed successfully.
    /// </summary>
    Completed,
}
