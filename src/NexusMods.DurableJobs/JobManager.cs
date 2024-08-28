using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.DurableJobs;
using NexusMods.DurableJobs.StateStore;

namespace NexusMods.DurableJobs;

public class JobManager : IJobManager
{
    private readonly object _lock = new();
    private ImmutableDictionary<JobId, JobHistory> _jobs = ImmutableDictionary<JobId, JobHistory>.Empty;
    public IServiceProvider ServiceProvider { get; }

    public JobManager(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Task<object> RunNew<TJob>(params object[] args)
        where TJob : AJob
    {
        var jobInstance = ServiceProvider.GetRequiredService<TJob>();
        var newJobId = JobId.From(Guid.NewGuid());
        
        var tcs = new TaskCompletionSource<object>();

        lock (_jobs)
        {
            _jobs = _jobs.Add(newJobId, new JobHistory
                {
                    JobId = newJobId,
                    JobType = typeof(TJob),
                    Continuation = Continuation,
                    Arguments = args,
                }
            );
        }

        Task.Run(() => StepJob(newJobId));

        return tcs.Task;
        

        void Continuation(object obj, Exception? ex)
        {
            if (ex != null)
            {
                tcs.SetException(ex);
            }
            else
            {
                tcs.SetResult(obj);
            }
        }
    }

    public Task<object> RunSubJob<TSubJob>(Context context, object[] args) where TSubJob : AJob
    {
        var parentJob = _jobs[context.JobId];
        if (context.HistoryIndex < parentJob.History.Count)
        {
            var historyEntry = parentJob.History[context.HistoryIndex];
            if (historyEntry.State == JobState.Waiting)
            {
                return Task.FromException<object>(new WaitException());
            }
            
            var resultData = parentJob.History[context.HistoryIndex].Result;

            if (historyEntry.State == JobState.Failed)
            {
                return Task.FromException<object>(new SubJobError(resultData.ToString()!));
            }
            
            var result = Task.FromResult(parentJob.History[context.HistoryIndex].Result);
            context.HistoryIndex++;
            return result;
        }
        
        var newJobId = JobId.From(Guid.NewGuid());
        
        lock (_jobs)
        {
            _jobs = _jobs.Add(newJobId, new JobHistory
                {
                    JobId = newJobId,
                    JobType = typeof(TSubJob),
                    Continuation = null!,
                    Arguments = args,
                    ParentJobId = context.JobId,
                }
            );
            
            
            _jobs = _jobs.SetItem(context.JobId, parentJob with
            {
                History = parentJob.History.Add(new HistoryEntry
                {
                    ChildJobId = newJobId,
                    State = JobState.Running,
                }),
                ChildJobs = parentJob.ChildJobs.Add(newJobId, (ushort)parentJob.History.Count),
            });
            Task.Run(() => StepJob(newJobId));
        }
        throw new WaitException();
    }
    
    private void SetJobState(JobId id, JobState state)
    {
        lock (_jobs)
        {
            _jobs = _jobs.SetItem(id, _jobs[id] with { State = state });
        }
    }

    private async Task StepJob(JobId jobId)
    {
        var varJobType = _jobs[jobId].JobType;
        var job = (AJob)ServiceProvider.GetRequiredService(varJobType);
        var history = _jobs[jobId];
        try
        {
            SetJobState(jobId, JobState.Running);
            var context = new Context
            {
                JobManager = this,
                JobId = jobId,
            };
            var result = await job.Run(context, history.Arguments);
            if (history.ParentJobId.HasValue) 
                NotifyParent(history.ParentJobId!.Value, jobId, result);
            history.Continuation?.Invoke(result, null);
            lock (_jobs)
            {
                _jobs = _jobs.Remove(jobId);
            }
        }
        catch (WaitException)
        {
            SetJobState(jobId, JobState.Waiting);
        }
        catch (Exception ex)
        {
            if (history.ParentJobId.HasValue) 
                NotifyParent(history.ParentJobId!.Value, jobId, null, ex);
            history.Continuation?.Invoke(null!, ex);
            lock (_jobs)
            {
                _jobs = _jobs.Remove(jobId);
            }
        }
    }

    private void NotifyParent(JobId parentJobId, JobId jobJobId, object? result, Exception? ex = null)
    {
        // If the parent job is not in the history, we can't notify it, it likely was already completed.
        if (!_jobs.TryGetValue(parentJobId, out var parentHistory))
            return;
        
        var parentIndex = parentHistory.ChildJobs[jobJobId];

        HistoryEntry newEntry;
        if (ex != null)
        {
            newEntry = new HistoryEntry
            {
                ChildJobId = jobJobId,
                State = JobState.Failed,
                Result = ex.Message,
            };
        }
        else
        {
            newEntry = new HistoryEntry
            {
                ChildJobId = jobJobId,
                State = JobState.Completed,
                Result = result!,
            };
        }

        lock (_jobs)
        {
            _jobs = _jobs.SetItem(parentJobId, parentHistory with
                {
                    History = parentHistory.History.SetItem(parentIndex, newEntry),
                }
            );
        }
        Task.Run(() => StepJob(parentJobId));
    }
}
