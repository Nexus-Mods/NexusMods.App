using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

[PublicAPI]
public abstract class AJobWorker : IJobWorker
{
    IJob IJobWorker.Job => Job;
    internal AJob Job { get; }

    protected AJobWorker(AJob job)
    {
        Job = job;
    }

    protected abstract Task<JobResult> ExecuteAsync(CancellationToken cancellationToken);

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        

        throw new NotImplementedException();
    }

    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task CancelAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    protected JobResult FailJob(Exception exception)
    {
        throw new NotImplementedException();
    }

    protected JobResult FailJob(string message)
    {
        throw new NotImplementedException();
    }

    protected JobResult CompleteJob<TData>(TData data)
    {
        throw new NotImplementedException();
    }
}

public abstract class AJobWorker<TJob> : AJobWorker
    where TJob : AJob
{
    protected new TJob Job { get; }

    protected AJobWorker(TJob job) : base(job)
    {
        Job = job;
    }
}

public static class Worker
{
    public static void AddFromStaticFunction<TJob, TOutput>(TJob job, Func<TJob, CancellationToken, Task<TOutput>> func)
        where TJob : AJob
    {
        throw new NotImplementedException();
    }
}
