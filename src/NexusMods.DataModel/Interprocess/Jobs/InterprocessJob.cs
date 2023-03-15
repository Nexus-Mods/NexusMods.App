using System.ComponentModel;
using System.Diagnostics;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Interprocess.Messages;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.RateLimiting;

namespace NexusMods.DataModel.Interprocess.Jobs;

/// <summary>
/// Base class for interprocess jobs.
/// </summary>
/// <typeparam name="T"></typeparam>
public class InterprocessJob : IInterprocessJob
{
    private readonly IInterprocessJobManager _manager;
    private Percent _progress;

    /// <summary>
    /// Create a new job, where the payload is a <see cref="IId"/>.
    /// </summary>
    /// <param name="jobType"></param>
    /// <param name="manager"></param>
    /// <param name="payload"></param>
    /// <param name="description"></param>
    public InterprocessJob(JobType jobType, IInterprocessJobManager manager, IId payload, string description) :
        this(jobType, manager, payload.ToTaggedBytes(), description)
    {
    }

    /// <summary>
    /// Create a new job, where the payload is a <see cref="LoadoutId"/>.
    /// </summary>
    /// <param name="jobType"></param>
    /// <param name="manager"></param>
    /// <param name="payload"></param>
    /// <param name="description"></param>
    public InterprocessJob(JobType jobType, IInterprocessJobManager manager, LoadoutId payload, string description) :
        this(jobType, manager, payload.ToArray(), description)
    {
    }

    /// <summary>
    /// Create a new job
    /// </summary>
    /// <param name="jobType"></param>
    /// <param name="manager"></param>
    /// <param name="payload"></param>
    /// <param name="description"></param>
    private InterprocessJob(JobType jobType, IInterprocessJobManager manager, byte[] payload, string description)
    {
        JobId = JobId.From(Guid.NewGuid());
        JobType = jobType;
        _manager = manager;
        Data = payload;
        Description = description;
        StartTime = DateTime.UtcNow;
        ProcessId = ProcessId.From((uint)Environment.ProcessId);
        _progress = Percent.Zero;
        _manager.CreateJob(this);
    }

    internal InterprocessJob(JobId jobId, IInterprocessJobManager manager, JobType jobType, ProcessId processId, string description, byte[] bytes, DateTime startTime, Percent progress)
    {
        JobId = jobId;
        _manager = manager;
        JobType = JobType;
        ProcessId = processId;
        Description = description;
        Data = bytes;
        StartTime = startTime;
        _progress = progress;
    }

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
    public string Description { get; }

    /// <inheritdoc />
    public JobType JobType { get; }

    /// <inheritdoc />
    public DateTime StartTime { get; }

    /// <inheritdoc />
    public byte[] Data { get; }

    public IId PayloadAsId => IId.FromTaggedSpan(Data);

    public void Dispose()
    {
        // If the process that created the job is still running, end the job.
        if (Environment.ProcessId == ProcessId.Value)
            _manager.EndJob(JobId);
    }
}
