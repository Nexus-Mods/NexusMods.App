using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Jobs;

[UsedImplicitly]
public sealed class JobMonitor : IJobMonitor, IDisposable
{
    private readonly SourceCache<IJob, JobId> _jobSourceCache = new(job => job.Id);
    private readonly ReadOnlyObservableCollection<IJob> _jobs;
    public ReadOnlyObservableCollection<IJob> Jobs => _jobs;

    private readonly CompositeDisposable _compositeDisposable = new();

    public JobMonitor()
    {
        var disposable = _jobSourceCache
            .Connect()
            .Bind(out _jobs)
            .Subscribe();

        _compositeDisposable.Add(disposable);
    }

    public IObservable<IChangeSet<TJob, JobId>> GetObservableChangeSet<TJob>() where TJob : IJob
    {
        return _jobSourceCache.Connect().OfType<IJob, JobId, TJob>();
    }

    public void RegisterJob(IJob job)
    {
        _jobSourceCache.AddOrUpdate(job);
    }

    public void Dispose()
    {
        _compositeDisposable.Dispose();
        _jobSourceCache.Dispose();
    }

    public IJobTask<TJobType, TResultType> Begin<TJobType, TResultType>(TJobType job, Func<IJobContext<TJobType>, ValueTask<TResultType>> task) where TJobType : IJobDefinition<TResultType>
    {
        throw new NotImplementedException();
    }
}
