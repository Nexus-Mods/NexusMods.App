using System.ComponentModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Interprocess.Messages;
using NexusMods.DataModel.RateLimiting;

namespace NexusMods.DataModel.Interprocess.Jobs;

/// <summary>
/// Base class for interprocess jobs.
/// </summary>
public class InterprocessJob : IInterprocessJob
{
    private readonly IInterprocessJobManager _manager;
    private Percent _progress;
    /// <summary>
    /// True if this instance is the "owning" instance of the job, where disposing
    /// the instance will auto-close the job
    /// </summary>
    private readonly bool _isOwner = true;

    /// <summary>
    /// Create a new job, where the payload is a IMessage
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="payload"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static InterprocessJob Create<T>(IInterprocessJobManager manager, T payload)
        where T : Entity
    {
        var job = new InterprocessJob(manager, payload);
        manager.CreateJob<T>(job);
        return job;
    }

    /// <summary>
    /// Create a new job
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="payload"></param>
    private InterprocessJob(IInterprocessJobManager manager, Entity payload)
    {
        JobId = JobId.From(Guid.NewGuid());
        _manager = manager;
        Payload = payload;
        StartTime = DateTime.UtcNow;
        ProcessId = Jobs.ProcessId.From((uint)Environment.ProcessId);
        _progress = Percent.Zero;
    }

    internal InterprocessJob(JobId jobId, IInterprocessJobManager manager, ProcessId processId, DateTime startTime, Percent progress, Entity payload)
    {
        JobId = jobId;
        _manager = manager;
        ProcessId = processId;
        Payload = payload;
        StartTime = startTime;
        _progress = progress;
        _isOwner = false;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public ProcessId ProcessId { get; }

    /// <inheritdoc />
    public JobId JobId { get; }

    /// <inheritdoc />
    public Percent Progress
    {
        get => _progress;
        set
        {
            _progress = value;
            _manager.UpdateProgress(JobId, value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress)));
        }
    }
    
    /// <inheritdoc />
    public DateTime StartTime { get; }

    /// <inheritdoc />
    public Entity Payload { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        // If the process that created the job is still running, end the job.
        if (_isOwner)
            _manager.EndJob(JobId);
    }
}
