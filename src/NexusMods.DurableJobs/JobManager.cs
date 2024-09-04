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
    private readonly Dictionary<Type,IJob> _jobInstances;
    private readonly IJobStateStore _jobStore;

    /// <summary>
    /// DI constructor.
    /// </summary>
    public JobManager(IServiceProvider serviceProvider)
    {
        _jobStore = serviceProvider.GetRequiredService<IJobStateStore>();
        _jobInstances = serviceProvider.GetServices<AOrchestration>().OfType<IJob>()
            .Concat(serviceProvider.GetServices<AUnitOfWork>())
            .ToDictionary(j => j.GetType());
        _logger = serviceProvider.GetRequiredService<ILogger<JobManager>>();
        _serializerOptions = serviceProvider.GetRequiredService<JsonSerializerOptions>();
    }
    
    
    
    /// <inheritdoc />
    public Task<object> RunNew<TJob>(params object[] args)
        where TJob : IJob
    {
        var jobInstance = _jobInstances[typeof(TJob)];
        var newJobId = JobId.From(Guid.NewGuid());
        
        var tcs = new TaskCompletionSource<object>();

        IActor<IJobMessage> actor;
        if (typeof(TJob).IsAssignableTo(typeof(AOrchestration)))
        {
            var initialState = new OrchestrationState
            {
                Job = jobInstance,
                Id = newJobId,
                Manager = this,
                Arguments = args,
                Continuation = Continuation,
            };

            actor = new JobActor(_logger, initialState);
        }
        else
        {
            var initialState = new UnitOfWorkState
            {
                Job = jobInstance,
                Id = newJobId,
                Manager = this,
                Arguments = args,
                Continuation = Continuation,
                CancellationTokenSource = new CancellationTokenSource(),
            };

            actor = new UnitOfWorkActor(_logger, initialState);
        }
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

    internal AJobState SaveState(AJobState state)
    {
        var memoryStream = new MemoryStream();
        var writer = new Utf8JsonWriter(memoryStream);
        JsonSerializer.Serialize(writer, state, _serializerOptions);
        _jobStore.Write(state.Id, memoryStream.ToArray());

#if DEBUG
        // In debug mode, we deserialize the state to ensure it can be deserialized properly.
        memoryStream.Position = 0;
        var newState = JsonSerializer.Deserialize<AJobState>(memoryStream, _serializerOptions)!;
        newState.Manager = state.Manager;
        newState.Continuation = state.Continuation;
        return newState;
#else
        return state;
#endif

    }

    internal void FinalizeJob(AJobState state, object result, bool isFailure)
    {
        state.Continuation?.Invoke(result, isFailure ? new SubJobError(result.ToString()!) : null);
        
        if (state.ParentJobId == JobId.DefaultValue)
            return;
        
        if (!_jobs.TryGetValue(state.ParentJobId, out var parentJob))
            return;
        
        parentJob.Post(new JobResultMessage(result, state.ParentHistoryIndex, isFailure));
        _jobs.TryRemove(state.Id, out _);
        _jobStore.Delete(state.Id);
    }

    public Task<object> RunSubJob<TSubJob>(OrchestrationContext context, object[] args) where TSubJob : IJob
    {
        // If we are replaying and our index is at the end of the history, we need to add a new entry and create a new job.
        if (context.ReplayIndex == context.History.Count)
        {
            if (typeof(TSubJob).IsAssignableTo(typeof(AOrchestration))) 
                CreateSubJobActor<TSubJob>(context, args);
            else if (typeof(TSubJob).IsAssignableTo(typeof(AUnitOfWork)))
                CreateUnitOfWorkActor<TSubJob>(context, args);
            else
                throw new InvalidOperationException("Unknown job type");
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

    private void CreateUnitOfWorkActor<TSubJob>(OrchestrationContext context, object[] args) where TSubJob : IJob
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
            
        var childJobActor = new UnitOfWorkActor(_logger, new UnitOfWorkState
        {
            Job = (AUnitOfWork)jobInstance,
            Id = jobId,
            Manager = this,
            Arguments = args,
            ParentJobId = context.JobId,
            ParentHistoryIndex = parentIdx,
            CancellationTokenSource = new CancellationTokenSource(),
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
    }

    private void CreateSubJobActor<TSubJob>(OrchestrationContext context, object[] args) where TSubJob : IJob
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
            
        var childJobActor = new JobActor(_logger, new OrchestrationState
        {
            Job = (AOrchestration)jobInstance,
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
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var jobsToStart = new List<JobId>();
        foreach (var job in _jobStore.All())
        {
            var state = JsonSerializer.Deserialize<AJobState>(job.Value, _serializerOptions)!;
            state.Manager = this;
            
            if (state is OrchestrationState js)
            {
                var actor = new JobActor(_logger, js);
                _jobs[state.Id] = actor;
            }
            else if (state is UnitOfWorkState uow)
            {
                var actor = new UnitOfWorkActor(_logger, uow);
                _jobs[state.Id] = actor;
            }
        }
        foreach (var toStart in jobsToStart)
        {
            _jobs[toStart].Post(RunMessage.Instance);
        }
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void SetProgress(JobId jobId, Percent? percent, double? ratePerSecond)
    {
        
    }
}
