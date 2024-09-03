namespace NexusMods.Abstractions.DurableJobs;

/// <summary>
/// An abstract, untyped unit of work.
/// </summary>
public abstract class AUnitOfWork : IJob
{
    /// <summary>
    /// Start the unit of work.
    /// </summary>
    public abstract Task<object> Start(OrchestrationContext context, object[] args, CancellationToken token);

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
    public override async Task<object> Start(OrchestrationContext context, object[] args, CancellationToken token)
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
