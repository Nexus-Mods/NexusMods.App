using System.Collections.ObjectModel;
using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;

namespace NexusMods.Jobs;

[UsedImplicitly]
public sealed class JobMonitor : IJobMonitor, IDisposable
{
    private static readonly AsyncLocal<IJobGroup> CurrentGroup = new();
    
    private readonly SourceCache<IJob, JobId> _allJobs = new(job => job.Id);
    private readonly ReadOnlyObservableCollection<IJob> _jobs;
    public ReadOnlyObservableCollection<IJob> Jobs => _jobs;

    private readonly CompositeDisposable _compositeDisposable = new();

    public JobMonitor()
    {
        var disposable = _allJobs
            .Connect()
            .Bind(out _jobs)
            .Subscribe();

        _compositeDisposable.Add(disposable);
    }

    public IObservable<IChangeSet<TJob, JobId>> GetObservableChangeSet<TJob>() where TJob : IJob
    {
        return _allJobs.Connect().OfType<IJob, JobId, TJob>();
    }

    public void RegisterJob(IJob job)
    {
        _allJobs.AddOrUpdate(job);
    }

    public void Dispose()
    {
        _compositeDisposable.Dispose();
        _allJobs.Dispose();
    }

    public IJobTask<TJobType, TResultType> Begin<TJobType, TResultType>(TJobType definition, Func<IJobContext<TJobType>, ValueTask<TResultType>> task) where TJobType : IJobDefinition<TResultType> 
        where TResultType : notnull
    {
        var ctx = new JobContext<TJobType, TResultType>(definition, this, null!, task);
        _allJobs.AddOrUpdate(ctx);
        Task.Run(ctx.Start);
        return new JobTask<TJobType, TResultType>(ctx);
    }

    public IJobTask<TJobType, TResultType> Begin<TJobType, TResultType>(TJobType job) where TJobType : IJobDefinitionWithStart<TJobType, TResultType> 
        where TResultType : notnull
    {
        var ctx = new JobContext<TJobType, TResultType>(job, this, null!, job.StartAsync);
        _allJobs.AddOrUpdate(ctx);
        Task.Run(ctx.Start);
        return new JobTask<TJobType, TResultType>(ctx);
    }
}
