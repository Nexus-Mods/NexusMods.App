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

        lock (_lock)
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
                tcs.TrySetException(ex);
            }
            else
            {
                tcs.TrySetResult(obj);
            }
        }
    }

    public Task<object> RunSubJob<TSubJob>(Context context, object[] args) where TSubJob : AJob
    {
        lock (_lock)
        {
            // Get the parent job's history.
            var parentJob = _jobs[context.JobId];

            // If the context handed to us by the parent job has a index that is less than the history count, then we know this sub job needs to be replayed
            // as it was waiting on a previous sub job to complete.
            if (context.HistoryIndex < parentJob.History.Count)
            {
                var historyEntry = parentJob.History[context.HistoryIndex];
                // If the job is still waiting, we return a task that will throw a WaitException, this allows the job to continue to run,
                // but throw an exception if the task is awaited
                if (historyEntry.State == JobState.Waiting)
                {
                    return Task.FromException<object>(new WaitException());
                }

                // If the job has ended, we return the result of the job.
                var resultData = parentJob.History[context.HistoryIndex].Result;

                // If the job has failed, we throw a SubJobError exception, this will be caught by the parent job
                if (historyEntry.State == JobState.Failed)
                {
                    return Task.FromException<object>(new SubJobError(resultData.ToString()!));
                }

                // If the job has completed, we return the result of the job.
                var result = Task.FromResult(parentJob.History[context.HistoryIndex].Result);
                context.HistoryIndex++;
                return result;
            }

            // Else we create a new job and add it to the job history.
            var newJobId = JobId.From(Guid.NewGuid());

            _jobs = _jobs.Add(newJobId, new JobHistory
                    {
                        JobId = newJobId,
                        JobType = typeof(TSubJob),
                        Continuation = null!,
                        Arguments = args,
                        ParentJobId = context.JobId,
                    }
                )
                .SetItem(context.JobId, parentJob with
                    {
                        History = parentJob.History.Add(new HistoryEntry
                            {
                                ChildJobId = newJobId,
                                State = JobState.Running,
                            }
                        ),
                        ChildJobs = parentJob.ChildJobs.Add(newJobId, (ushort)parentJob.History.Count),
                    }
                );
            Task.Run(() => StepJob(newJobId));

            // The sub job is now running, we return a task that will throw a WaitException in the parent.
            return Task.FromException<object>(new WaitException());
        }
    }
    
    private void SetJobState(JobId id, JobState state)
    {
        lock (_lock)
        {
            _jobs = _jobs.SetItem(id, _jobs[id] with { State = state });
        }
    }

    private async Task StepJob(JobId jobId)
    {
        // Get the job history and create a new instance of the job.
        if (!_jobs.TryGetValue(jobId, out var history))
            return;
        var job = (AJob)ServiceProvider.GetRequiredService(history.JobType);
        
        try
        {
            // Set the job state to running, and run the job.
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
            lock (_lock)
            {
                _jobs = _jobs.Remove(jobId);
            }
        }
        catch (WaitException)
        {
            // Set the job state to waiting, and return.
            SetJobState(jobId, JobState.Waiting);
        }
        catch (Exception ex)
        {
            // Another exception occurred, set the job state to failed, and notify the parent job.
            if (history.ParentJobId.HasValue) 
                NotifyParent(history.ParentJobId!.Value, jobId, null, ex);
            history.Continuation?.Invoke(null!, ex);
            lock (_lock)
            {
                _jobs = _jobs.Remove(jobId);
            }
        }
    }

    private void NotifyParent(JobId parentJobId, JobId jobJobId, object? result, Exception? ex = null)
    {
        lock (_lock) {
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

            _jobs = _jobs.SetItem(parentJobId, parentHistory with
                {
                    History = parentHistory.History.SetItem(parentIndex, newEntry),
                }
            );

            Task.Run(() => StepJob(parentJobId));
        }
    }
}
