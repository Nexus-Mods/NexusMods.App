using System.Collections.ObjectModel;
using DynamicData;
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
    void CreateJob(IInterprocessJob job);

    /// <summary>
    /// Marks the job as completed and deletes it from the job manager.
    /// </summary>
    /// <param name="job"></param>
    void EndJob(JobId job);

    void UpdateProgress(JobId jobId, Percent value);

    IObservable<IChangeSet<IInterprocessJob,JobId>> Jobs { get; }
}
