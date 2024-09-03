namespace NexusMods.Abstractions.DurableJobs;

/// <summary>
/// A durable job that combines and orchestrates multiple sub jobs.
/// </summary>
public abstract class AOrchestration : IJob
{ 
    /// <summary>
    /// Runs the orchestration. Calls in this method to start sub-jobs may pause the orchestration, and the orchestration may restart
    /// many times before it finishes. All code in this method should be idempotent.
    /// </summary>
    internal abstract Task<object> Run(Context context, params object[] args);

    /// <inheritdoc />
    public abstract Type ResultType { get; }
    
    /// <inheritdoc />
    public abstract Type[] ArgumentTypes { get; }
}

/// <summary>
/// A job with 1 argument, and a result.
/// </summary>
public abstract class AOrchestration<TParent, TResult, TArg1> : AOrchestration
  where TParent : AOrchestration<TParent, TResult, TArg1>
{
    /// <summary>
    /// The main entry point for the job, this will be called multiple times until the job is completed.
    /// </summary>
    protected abstract Task<TResult> Run(Context context, TArg1 arg1);

    internal override async Task<object> Run(Context context, params object[] args)
    {
        if (args.Length != 1)
        {
            throw new ArgumentException("Expected 1 argument", nameof(args));
        }

        return (await Run(context, (TArg1)args[0]))!;
    }

    /// <inheritdoc />
    public override Type ResultType => typeof(TResult);
    
    /// <inheritdoc />
    public override Type[] ArgumentTypes => [typeof(TArg1)];


    /// <summary>
    /// Runs this job as a sub job of the given parent job.
    /// </summary>
    protected static async Task<TResult> RunSubJob(Context parentContext, TArg1 arg1)
    {
        return (TResult)await parentContext.JobManager.RunSubJob<TParent>(parentContext, [arg1!]);
    }
}
