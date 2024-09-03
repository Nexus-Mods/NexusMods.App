namespace NexusMods.Abstractions.DurableJobs;

/// <summary>
/// An abstract, untyped unit of work.
/// </summary>
public abstract class AUnitOfWork : IJob
{
    /// <summary>
    /// Start the unit of work.
    /// </summary>
    public abstract Task<object> Start(object[] args, CancellationToken token);

    /// <inheritdoc />
    public abstract Type ResultType { get; }
    
    /// <inheritdoc />
    public abstract Type[] ArgumentTypes { get; }
}


/// <summary>
/// A unit of work with 1 argument, and a result.
/// </summary>
public abstract class AUnitOfWork<TParent, TResult, TArg1> : AUnitOfWork
    where TParent : AUnitOfWork<TParent, TResult, TArg1>
{
    /// <summary>
    /// The main entry point for the unit of work.
    /// </summary>
    protected abstract Task<TResult> Start(TArg1 arg1, CancellationToken token);
    
    /// <inheritdoc />
    public override Type ResultType => typeof(TResult);
    
    /// <inheritdoc />
    public override Type[] ArgumentTypes => [typeof(TArg1)];

    /// <summary>
    /// Start the unit of work.
    /// </summary>
    public override async Task<object> Start(object[] args, CancellationToken token)
    {
        if (args.Length != 1)
        {
            throw new ArgumentException("Expected 1 argument", nameof(args));
        }

        return (await Start((TArg1)args[0], token))!;
    }
    
    /// <summary>
    /// Runs this job as a sub job of the given parent job.
    /// </summary>
    public static async Task<TResult> RunUnitOfWork(OrchestrationContext parentOrchestrationContext, TArg1 arg1)
    {
        return (TResult)await parentOrchestrationContext.JobManager.RunSubJob<TParent>(parentOrchestrationContext, [arg1!]);
    }
}


/// <summary>
/// A unit of work with 1 argument, and a result.
/// </summary>
public abstract class AUnitOfWork<TParent, TResult, TArg1, TArg2> : AUnitOfWork, IJob<TResult, TArg1, TArg2>
    where TParent : AUnitOfWork<TParent, TResult, TArg1, TArg2>
{
    /// <summary>
    /// The main entry point for the unit of work.
    /// </summary>
    protected abstract Task<TResult> Start(TArg1 arg1, TArg2 arg2, CancellationToken token);
    
    /// <inheritdoc />
    public override Type ResultType => typeof(TResult);
    
    /// <inheritdoc />
    public override Type[] ArgumentTypes => [typeof(TArg1), typeof(TArg2)];

    /// <summary>
    /// Start the unit of work.
    /// </summary>
    public override async Task<object> Start(object[] args, CancellationToken token)
    {
        if (args.Length != 2)
        {
            throw new ArgumentException("Expected 2 arguments", nameof(args));
        }

        return (await Start((TArg1)args[0], (TArg2)args[1], token))!;
    }
    
    /// <summary>
    /// Runs this job as a sub job of the given parent job.
    /// </summary>
    public static async Task<TResult> RunUnitOfWork(OrchestrationContext parentOrchestrationContext, TArg1 arg1, TArg2 arg2)
    {
        return (TResult)await parentOrchestrationContext.JobManager.RunSubJob<TParent>(parentOrchestrationContext, [arg1!, arg2!]);
    }
    
    /// <summary>
    /// Runs this job as a new job.
    /// </summary>
    public static async Task<TResult> RunNew(IJobManager manager, TArg1 arg1, TArg2 arg2)
    {
        return (TResult)await manager.RunNew<TParent>([arg1!, arg2!]);
    }
    
    /// <summary>
    /// Gets all running jobs of this type.
    /// </summary>
    public static IEnumerable<(JobId Id, TArg1 Arg1, TArg2 Arg2)> AllRunning(IJobManager jobManager)
    {
        return jobManager.GetJobReports()
            .Where(t => t.Type == typeof(TParent))
            .Select(t => (t.JobId, (TArg1)t.Args[0], (TArg2)t.Args[1]));
    }
}
