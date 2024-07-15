using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

[PublicAPI]
public static class JobWorker
{
    public delegate Task<TData> ExecuteAsyncDelegateWithData<TJob, TData>(TJob job, AJobWorker<TJob> worker, CancellationToken cancellationToken)
        where TJob : AJob
        where TData : notnull;

    public delegate Task<JobResult> ExecuteAsyncDelegate<TJob>(TJob job, AJobWorker<TJob> worker, CancellationToken cancellationToken)
        where TJob : AJob;

    public static AJobWorker<TJob> Create<TJob, TData>(TJob job, ExecuteAsyncDelegateWithData<TJob, TData> func)
    where TJob : AJob
    where TData : notnull
    {
        return new DummyWorker<TJob, TData>(func);
    }

    public static AJobWorker<TJob> Create<TJob>(TJob job, ExecuteAsyncDelegate<TJob> func)
        where TJob : AJob
    {
        return new DummyWorker<TJob>(func);
    }

    private class DummyWorker<TJob> : AJobWorker<TJob>
        where TJob : AJob
    {
        private readonly ExecuteAsyncDelegate<TJob> _func;

        public DummyWorker(ExecuteAsyncDelegate<TJob> func)
        {
            _func = func;
        }

        protected override Task<JobResult> ExecuteAsync(TJob job, CancellationToken cancellationToken)
        {
            return _func.Invoke(job, this, cancellationToken);
        }
    }

    private class DummyWorker<TJob, TData> : AJobWorker<TJob>
        where TJob : AJob
        where TData : notnull
    {
        private readonly ExecuteAsyncDelegateWithData<TJob, TData> _func;

        public DummyWorker(ExecuteAsyncDelegateWithData<TJob, TData> func)
        {
            _func = func;
        }

        protected override async Task<JobResult> ExecuteAsync(TJob job, CancellationToken cancellationToken)
        {
            var result = await _func.Invoke(job, this, cancellationToken);
            return JobResult.CreateCompleted(result);
        }
    }
}

