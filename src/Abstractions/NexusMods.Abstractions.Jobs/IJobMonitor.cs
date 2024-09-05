namespace NexusMods.Abstractions.Jobs;

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
}
