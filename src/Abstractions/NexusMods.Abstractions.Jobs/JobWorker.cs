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

    public static AJobWorker<TJob> CreateWithData<TJob, TData>(TJob job, ExecuteAsyncDelegateWithData<TJob, TData> func)
    where TJob : AJob
    where TData : notnull
    {
        return new DummyWorker<TJob, TData>(job, func);
    }

    public static AJobWorker<TJob> Create<TJob>(TJob job, ExecuteAsyncDelegate<TJob> func)
        where TJob : AJob
    {
        return new DummyWorker<TJob>(job, func);
    }

    private class DummyWorker<TJob> : AJobWorker<TJob>
        where TJob : AJob
    {
        private readonly ExecuteAsyncDelegate<TJob> _func;

        public DummyWorker(TJob job, ExecuteAsyncDelegate<TJob> func)
        {
            _func = func;
            SetWorker(job);
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

        public DummyWorker(TJob job, ExecuteAsyncDelegateWithData<TJob, TData> func)
        {
            _func = func;
            SetWorker(job);
        }

        protected override async Task<JobResult> ExecuteAsync(TJob job, CancellationToken cancellationToken)
        {
            var result = await _func.Invoke(job, this, cancellationToken);
            return JobResult.CreateCompleted(result);
        }
    }
}

