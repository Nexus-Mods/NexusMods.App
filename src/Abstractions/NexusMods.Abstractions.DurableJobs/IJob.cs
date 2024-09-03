namespace NexusMods.Abstractions.DurableJobs;

/// <summary>
/// Common interface for all jobs.
/// </summary>
public interface IJob
{
    
    /// <summary>
    /// The result type of this job.
    /// </summary>
    public Type ResultType { get; }
    
    /// <summary>
    /// The argument types of this job.
    /// </summary>
    public Type[] ArgumentTypes { get; }
}


public interface IJob<TResult, TArg1, TArg2>
{
    
}
