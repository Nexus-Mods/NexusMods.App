using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DurableJobs;
namespace NexusMods.DurableJobs;

/// <summary>
/// Implementation of the job manager.
/// </summary>
public class JobManager : IJobManager, IHostedService
{
    private readonly ConcurrentDictionary<JobId, IActor<IJobMessage>> _jobs = new();
    private readonly ILogger<JobManager> _logger;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly Dictionary<Type,AJob> _jobInstances;
    private readonly IJobStateStore _jobStore;

    /// <summary>
    /// DI constructor.
    /// </summary>
    public JobManager(IServiceProvider serviceProvider)
    {
        _jobStore = serviceProvider.GetRequiredService<IJobStateStore>();
        _jobInstances = serviceProvider.GetServices<AJob>().ToDictionary(j => j.GetType());
        _logger = serviceProvider.GetRequiredService<ILogger<JobManager>>();
        _serializerOptions = serviceProvider.GetRequiredService<JsonSerializerOptions>();
    }
    
    
    
    /// <inheritdoc />
    public Task<object> RunNew<TJob>(params object[] args)
        where TJob : AJob
    {
        var jobInstance = _jobInstances[typeof(TJob)];
        var newJobId = JobId.From(Guid.NewGuid());
        
        var tcs = new TaskCompletionSource<object>();

        var initialState = new JobState
        {
            Job = jobInstance,
            Id = newJobId,
            Manager = this,
            Arguments = args,
            Continuation = Continuation,
        };
        
        var actor = new JobActor(_logger, initialState);
        actor.Post(RunMessage.Instance);
        _jobs[newJobId] = actor;
        
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
    
    /// <summary>
    /// Cancel the given job, this will trigger a cancel message to be sent to all of its children and parents.
    /// </summary>
    internal void CancelJob(JobId stateParentJobId)
    {
        if (!_jobs.TryGetValue(stateParentJobId, out var parentJob))
            return;
        parentJob.Post(CancelMessage.Instance);
    }

    internal JobState SaveState(JobState state)
    {
        var memoryStream = new MemoryStream();
        var writer = new Utf8JsonWriter(memoryStream);
        JsonSerializer.Serialize(writer, state, _serializerOptions);
        _jobStore.Write(state.Id, memoryStream.ToArray());

#if DEBUG
        // In debug mode, we deserialize the state to ensure it can be deserialized properly.
        memoryStream.Position = 0;
        var newState = JsonSerializer.Deserialize<JobState>(memoryStream)!;
        newState.Manager = state.Manager;
        newState.Continuation = state.Continuation;
        return newState;
#else
        return state;
#endif

    }

    internal void FinalizeJob(JobState state, object result, bool isFailure)
    {
        state.Continuation?.Invoke(result, isFailure ? new SubJobError((string)result) : null);
        
        if (state.ParentJobId == JobId.Empty)
            return;
        
        if (!_jobs.TryGetValue(state.ParentJobId, out var parentJob))
            return;
        
        parentJob.Post(new JobResultMessage(result, state.ParentHistoryIndex, isFailure));
        _jobs.TryRemove(state.Id, out _);
        _jobStore.Delete(state.Id);
    }

    public Task<object> RunSubJob<TSubJob>(Context context, object[] args) where TSubJob : IJob
    {
        // If we are replaying and our index is at the end of the history, we need to add a new entry and create a new job.
        if (context.ReplayIndex == context.History.Count)
        {
            var jobId = JobId.From(Guid.NewGuid());
            var jobInstance = _jobInstances[typeof(TSubJob)];
            var entry = new HistoryEntry
            {
                ChildJobId = jobId,
                Status = JobStatus.Running,
            };
            var parentIdx = context.History.Count;
            context.History.Add(entry);
            
            var childJobActor = new JobActor(_logger, new JobState
            {
                Job = jobInstance,
                Id = jobId,
                Manager = this,
                Arguments = args,
                ParentJobId = context.JobId,
                ParentHistoryIndex = parentIdx,
            });
            _jobs[jobId] = childJobActor;
            childJobActor.Post(RunMessage.Instance);
            
            // Delete the job when it's done.
            childJobActor.StatusObservable.Subscribe(status =>
            {
                if (status != ActorStatus.Stopped) return;
                _jobs.TryRemove(jobId, out _);
                _jobStore.Delete(jobId);
            });

            context.ReplayIndex++;
            return Task.FromException<object>(new WaitException());
        }
        if (context.ReplayIndex < context.History.Count)
        {
            var entry = context.History[context.ReplayIndex];
            context.ReplayIndex++;
            return entry.Status switch
            {
                JobStatus.Completed => Task.FromResult(entry.Result!),
                JobStatus.Failed => Task.FromException<object>(new SubJobError((string)entry.Result!)),
                _ => Task.FromException<object>(new WaitException()),
            };
        }
        throw new InvalidOperationException("Replay index is out of bounds");
    }
    
    /// <inheritdoc />
    public Task<object> RunUnitOfWork<TUnitOfWork>(Context parent, object[] args) where TUnitOfWork : AUnitOfWork
    {
        throw new NotImplementedException();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LoadJobs();
        RestartJobs();
    }

    private void RestartJobs()
    {
        throw new NotImplementedException();
    }

    private void LoadJobs()
    {
        foreach (var job in _jobStore.All())
        {
            var state = JsonSerializer.Deserialize<JobState>(job.Value, _serializerOptions)!;
            state.Manager = this;
            var actor = new JobActor(_logger, state);
            _jobs[state.Id] = actor;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
