using System.Collections.ObjectModel;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<JobMonitor> _logger;

    public JobMonitor(ILogger<JobMonitor> logger)
    {
        _logger = logger;
        var disposable = _allJobs
            .Connect()
            .Bind(out _jobs)
            .Subscribe();

        _compositeDisposable.Add(disposable);
    }

    public IObservable<IChangeSet<IJob, JobId>> GetObservableChangeSet<TJobDefinition>() where TJobDefinition : IJobDefinition
    {
        return _allJobs.Connect()
            .Filter(j => j.Definition is TJobDefinition);
    }

    public void Dispose()
    {
        _compositeDisposable.Dispose();
        _allJobs.Dispose();
    }

    public IJobTask<TJobType, TResultType> Begin<TJobType, TResultType>(TJobType definition, Func<IJobContext<TJobType>, ValueTask<TResultType>> task) where TJobType : IJobDefinition<TResultType> 
        where TResultType : notnull
    {
        var group = new JobGroup();
        var ctx = new JobContext<TJobType, TResultType>(definition, this, group, group.JobCancellationToken, task);
        _allJobs.AddOrUpdate(ctx);
        Task.Run(async () =>
            {
                await ctx.Start();
                if (ctx.Status == JobStatus.Failed)
                {
                    if (ctx.TryGetException(out var ex))
                    {
                        _logger.LogError(ex!, "Job {JobId} of type {JobType} failed", ctx.Id, typeof(TJobType));
                    }
                    else
                    {
                        _logger.LogError("Job {JobId} of type {JobType} failed", ctx.Id, typeof(TJobType));
                    }
                }
                _allJobs.Remove(ctx);
            }
        );
        return new JobTask<TJobType, TResultType>(ctx);
    }

    public IJobTask<TJobType, TResultType> Begin<TJobType, TResultType>(TJobType job) where TJobType : IJobDefinitionWithStart<TJobType, TResultType> 
        where TResultType : notnull
    {
        var group = new JobGroup();
        var ctx = new JobContext<TJobType, TResultType>(job, this, group, group.JobCancellationToken, job.StartAsync);
        _allJobs.AddOrUpdate(ctx);
        Task.Run(async () =>
            {
                await ctx.Start();
                if (ctx.Status == JobStatus.Failed)
                {
                    if (ctx.TryGetException(out var ex))
                    {
                        _logger.LogError(ex!, "Job {JobId} of type {JobType} failed", ctx.Id, typeof(TJobType));
                    }
                    else
                    {
                        _logger.LogError("Job {JobId} of type {JobType} failed", ctx.Id, typeof(TJobType));
                    }
                }
                _allJobs.Remove(ctx);
            }
        );
        return new JobTask<TJobType, TResultType>(ctx);
    }

    public void Cancel(JobId jobId)
    {
        var job = _allJobs.Lookup(jobId);
        if (job.HasValue)
            job.Value.Cancel();
    }
    
    public void Cancel(IJobTask jobTask) => jobTask.Job.Cancel();

    public void CancelGroup(IJobGroup group) => group.Cancel();

    public void CancelAll()
    {
        foreach (var job in _allJobs.Items)
        {
            if (job.Status.IsActive())
                job.Cancel();
        }
    }
    
    public void Pause(JobId jobId)
    {
        var job = _allJobs.Lookup(jobId);
        if (job.HasValue)
            job.Value.Pause();
    }
    
    public void Pause(IJobTask jobTask) => jobTask.Job.Pause();
    
    public void PauseGroup(IJobGroup group) => group.Pause();
    
    public void PauseAll()
    {
        foreach (var job in _allJobs.Items)
        {
            if (job.Status == JobStatus.Running)
                job.Pause();
        }
    }
    
    public void Resume(JobId jobId)
    {
        var job = _allJobs.Lookup(jobId);
        if (job.HasValue)
            job.Value.Resume();
    }
    
    public void Resume(IJobTask jobTask) => jobTask.Job.Resume();
    
    public void ResumeGroup(IJobGroup group) => group.Resume();
    
    public void ResumeAll()
    {
        foreach (var job in _allJobs.Items)
        {
            if (job.Status == JobStatus.Paused)
                job.Resume();
        }
    }
}
