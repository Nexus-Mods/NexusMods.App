using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Text;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Interprocess.Messages;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Paths.Extensions;

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
    /// True if this instance is the "owning" instance of the job, where disposing
    /// the instance will auto-close the job
    /// </summary>
    private readonly bool _isOwner = true;

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
    /// Create a new job, where the payload is a <see cref="Uri"/>.
    /// </summary>
    /// <param name="jobType"></param>
    /// <param name="manager"></param>
    /// <param name="payload"></param>
    /// <param name="description"></param>
    public InterprocessJob(JobType jobType, IInterprocessJobManager manager, Uri payload, string description) :
        this(jobType, manager, Encoding.UTF8.GetBytes(payload.ToString()), description)
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
    /// Create a new job, where the payload is a IMessage
    /// </summary>
    /// <param name="jobType"></param>
    /// <param name="manager"></param>
    /// <param name="payload"></param>
    /// <param name="description"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static InterprocessJob Create<T>(JobType jobType, IInterprocessJobManager manager, T payload, string description)
        where T : IMessage
    {
        if (T.MaxSize <= 512)
        {
            Span<byte> data = stackalloc byte[T.MaxSize];
            var used = payload.Write(data);
            var payloadArray = data.SliceFast(0, used).ToArray();
            return new InterprocessJob(jobType, manager, payloadArray,
                description);
        }
        else
        {
            var payloadArray = new byte[T.MaxSize];
            var used = payload.Write(payloadArray);
            return new InterprocessJob(jobType, manager, payloadArray.AsSpan().SliceFast(0, used).ToArray(),
                description);
        }
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
        JobType = jobType;
        ProcessId = processId;
        Description = description;
        Data = bytes;
        StartTime = startTime;
        _progress = progress;
        _isOwner = false;
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

    /// <inheritdoc />
    public IId PayloadAsId => IId.FromTaggedSpan(Data);

    /// <inheritdoc />
    public Uri PayloadAsUri => new (Encoding.UTF8.GetString(Data));

    /// <inheritdoc />
    public LoadoutId LoadoutId => LoadoutId.From(Data);

    /// <inheritdoc />
    public T PayloadAsIMessage<T>() where T : IMessage, new()
    {
        return (T)T.Read(Data);
    }

    public void Dispose()
    {
        // If the process that created the job is still running, end the job.
        if (_isOwner)
            _manager.EndJob(JobId);
    }
}
