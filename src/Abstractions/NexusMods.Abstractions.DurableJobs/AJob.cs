using System.Runtime.CompilerServices;

namespace NexusMods.Abstractions.DurableJobs;

/// <summary>
/// A durable job that can be paused, resumed, and restarted.
/// </summary>
public abstract class AJob
{ 
    /// <summary>
    /// Runs the job. Calls in this method to <see cref="RunSubJob"/> may pause the job, and the job may restart
    /// many times before it finishes. All code in this method should be idempotent.
    /// </summary>
    internal abstract Task<object> Run(Context context, params object[] args);

    /// <summary>
    /// The result type of this job.
    /// </summary>
    public abstract Type ResultType { get; }
    
    /// <summary>
    /// The argument types of this job.
    /// </summary>
    public abstract Type[] ArgumentTypes { get; }
}

/// <summary>
/// A job with 1 argument, and a result.
/// </summary>
public abstract class AJob<TParent, TResult, TArg1> : AJob
  where TParent : AJob<TParent, TResult, TArg1>
{
    /// <summary>
    /// The main entry point for the job, this will be called multiple times until the job is completed.
    /// </summary>
    protected abstract Task<TResult> Run(Context context, TArg1 arg1);

    internal override async Task<object> Run(Context context, params object[] args)
    {
        if (args.Length != 1)
        {
            throw new ArgumentException("Expected 2 arguments", nameof(args));
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
