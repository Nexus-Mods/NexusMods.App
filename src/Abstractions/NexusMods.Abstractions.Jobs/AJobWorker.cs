using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

[PublicAPI]
public abstract class AJobWorker : IJobWorker
{
    public IControllableJob Job { get; }

    protected AJobWorker(IControllableJob job)
    {
        Job = job;
    }

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

    [DoesNotReturn]
    protected void FailJob(Exception exception)
    {
        throw new NotImplementedException();
    }

    [DoesNotReturn]
    protected void FailJob(string message)
    {
        throw new NotImplementedException();
    }
}

public abstract class AJobWorker<TJob> : AJobWorker
    where TJob : IMutableJob
{
    protected new TJob Job { get; }

    protected AJobWorker(TJob job) : base(job)
    {
        Job = job;
    }
}

public abstract class AJobGroupWorker<TJobGroup> : AJobWorker<TJobGroup>
    where TJobGroup : IMutableJobGroup
{
    protected TJobGroup JobGroup { get; }

    protected AJobGroupWorker(TJobGroup jobGroup) : base(jobGroup)
    {
        JobGroup = jobGroup;
    }

    protected Task<JobResult> AddJobAndWaitAsync(IMutableJob job)
    {
        throw new NotImplementedException();
    }
}

public static class Worker
{
    public static IJobWorker CreateFromStaticFunction<TJob, TOutput>(TJob job, Func<TJob, CancellationToken, Task<TOutput>> func)
        where TJob : IMutableJob
    {
        throw new NotImplementedException();
    }
}
