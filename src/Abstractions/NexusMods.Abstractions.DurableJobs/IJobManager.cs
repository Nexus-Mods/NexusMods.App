using DynamicData;
using ObservableCollections;

namespace NexusMods.Abstractions.DurableJobs;

/// <summary>
/// Public interface for the job manager.
/// </summary>
public interface IJobManager
{
    /// <summary>
    /// The main entry point for the job manager, don't run this inside of any job context. 
    /// </summary>
    public Task<object> RunNew<TJob>(params object[] args) where TJob : IJob;
    
    /// <summary>
    /// Runs a sub job from the parent job.
    /// </summary>
    Task<object> RunSubJob<TSubJob>(OrchestrationContext parent, object[] args) where TSubJob : IJob;

    /// <summary>
    /// Gets a job report for all jobs.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<JobReport> GetJobReports();
}

public record JobReport(JobId JobId, Type Type, object[] Args);
