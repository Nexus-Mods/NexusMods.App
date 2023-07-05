using DynamicData;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Interprocess.Messages;
using NexusMods.DataModel.RateLimiting;

namespace NexusMods.DataModel.Interprocess.Jobs;

/// <summary>
/// Defines a job manager that tracks progress and tracking of jobs across
/// processes. This can be used to block one instance of the app from deploying
/// to a folder while another instance is already deploying to the same folder.
/// </summary>
public interface IInterprocessJobManager
{
    /// <summary>
    /// Create a job with a payload of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="job"></param>
    /// <typeparam name="T"></typeparam>
    void CreateJob<T>(IInterprocessJob job) where T : Entity;

    /// <summary>
    /// Marks the job as completed and deletes it from the job manager.
    /// </summary>
    /// <param name="job"></param>
    void EndJob(JobId job);

    /// <summary>
    /// Update the progress of a given job, this will be eventually reflected
    /// in any other processes that are tracking the job.
    /// </summary>
    /// <param name="jobId"></param>
    /// <param name="value"></param>
    void UpdateProgress(JobId jobId, Percent value);

    /// <summary>
    /// All jobs that are currently being tracked on this machine.
    /// </summary>
    IObservable<IChangeSet<IInterprocessJob,JobId>> Jobs { get; }
}
