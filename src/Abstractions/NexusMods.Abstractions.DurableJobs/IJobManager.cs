using DynamicData;

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
}
