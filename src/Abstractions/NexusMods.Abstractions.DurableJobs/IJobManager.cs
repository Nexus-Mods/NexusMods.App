namespace NexusMods.Abstractions.DurableJobs;

public interface IJobManager
{
    /// <summary>
    /// The main entry point for the job manager, don't run this inside of any job context. 
    /// </summary>
    public Task<object> RunNew<TJob>(params object[] args) where TJob : AJob;

    /// <summary>
    /// Runs a sub job from the parent job.
    /// </summary>
    Task<object> RunSubJob<TSubJob>(Context parent, object[] args) where TSubJob : AJob;
}
