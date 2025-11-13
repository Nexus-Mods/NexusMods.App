using System.Collections.ObjectModel;
using DynamicData;

namespace NexusMods.Sdk.Jobs;

/// <summary>
/// A monitor for jobs
/// </summary>
public interface IJobMonitor
{
    /// <summary>
    /// Starts a job given the job definition and the code to run as part of the job.
    /// </summary>
    IJobTask<TJobType, TResultType> Begin<TJobType, TResultType>(TJobType job, Func<IJobContext<TJobType>, ValueTask<TResultType>> task)
        where TJobType : IJobDefinition<TResultType>
        where TResultType : notnull;

    /// <summary>
    /// Starts a job given the job definition.
    /// </summary>
    IJobTask<TJobType, TResultType> Begin<TJobType, TResultType>(TJobType job)
        where TJobType : IJobDefinitionWithStart<TJobType, TResultType>
        where TResultType : notnull;
    
    /// <summary>
    /// Gets an observable with changeset for jobs of type <typeparamref name="TJob"/>.
    /// </summary>
    IObservable<IChangeSet<IJob, JobId>> GetObservableChangeSet<TJob>() where TJob : IJobDefinition;
    
    /// <summary>
    /// All the jobs the monitor knows about
    /// </summary>
    ReadOnlyObservableCollection<IJob> Jobs { get; }
    
    /// <summary>
    /// Cancels a specific job by its ID
    /// </summary>
    void Cancel(JobId jobId);
    
    /// <summary>
    /// Cancels a specific job by its task
    /// </summary>
    void Cancel(IJobTask jobTask);
    
    /// <summary>
    /// Cancels all jobs in the specified group
    /// </summary>
    void CancelGroup(IJobGroup group);
    
    /// <summary>
    /// Cancels all active jobs
    /// </summary>
    void CancelAll();
    
    /// <summary>
    /// Pauses a specific job by its ID
    /// </summary>
    void Pause(JobId jobId);
    
    /// <summary>
    /// Pauses a specific job by its task
    /// </summary>
    void Pause(IJobTask jobTask);
    
    /// <summary>
    /// Pauses all jobs in the specified group
    /// </summary>
    void PauseGroup(IJobGroup group);
    
    /// <summary>
    /// Pauses all active jobs
    /// </summary>
    void PauseAll();
    
    /// <summary>
    /// Pauses download queue to prevent new download jobs from starting
    /// </summary>
    void PauseDownloadQueue();
    
    /// <summary>
    /// Resumes a specific job by its ID
    /// </summary>
    void Resume(JobId jobId);
    
    /// <summary>
    /// Resumes a specific job by its task
    /// </summary>
    void Resume(IJobTask jobTask);
    
    /// <summary>
    /// Resumes all jobs in the specified group
    /// </summary>
    void ResumeGroup(IJobGroup group);
    
    /// <summary>
    /// Resumes all active jobs
    /// </summary>
    void ResumeAll();
    
    /// <summary>
    /// Resumes download queue to allow new download jobs to start
    /// </summary>
    void ResumeDownloadQueue();
    
    /// <summary>
    /// Returns whether the download queue is currently paused
    /// </summary>
    bool IsDownloadQueuePaused();
    
    /// <summary>
    /// Finds a job by its ID
    /// </summary>
    /// <param name="jobId">The ID of the job to find</param>
    /// <returns>The job with the specified ID, or null if not found</returns>
    IJob? Find(JobId jobId);
}
