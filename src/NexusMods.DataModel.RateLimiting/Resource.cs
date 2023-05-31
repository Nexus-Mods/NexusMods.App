using System.Collections.Concurrent;
using System.Numerics;
using System.Threading.Channels;

namespace NexusMods.DataModel.RateLimiting;

/// <summary>
/// Standard implementation for an individual 'resource'. Using this class you can
/// create jobs and report info about them. The methods may delay returning based on the current
/// rate limits of the resource.
/// </summary>
/// <typeparam name="TResource">A marker for the service owning this limiter</typeparam>
/// <typeparam name="TUnit">The unit of measurement of job size</typeparam>
public class Resource<TResource, TUnit> : IResource<TResource, TUnit>
    where TUnit : IAdditionOperators<TUnit, TUnit, TUnit>,
    IAdditiveIdentity<TUnit, TUnit>, IDivisionOperators<TUnit, TUnit, double>,
    IEqualityOperators<TUnit, TUnit, bool>
{
    private readonly Channel<PendingReport> _channel;
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentDictionary<ulong, Job<TResource, TUnit>> _tasks;
    private readonly IDateTimeProvider _provider;
    private ulong _nextId;
    private TUnit _totalUsed;

    /// <inheritdoc />
    public IEnumerable<IJob> Jobs => _tasks.Values;

    /// <summary>
    /// Creates a new resource using the default settings.
    /// </summary>
    /// <param name="humanName">Human friendly name for this resource.</param>
    /// <remarks>
    /// Default settings limit max jobs to CPU threads and no throughput limit.
    /// </remarks>
    public Resource(string humanName) : this(humanName, 0, TUnit.AdditiveIdentity) { }

    /// <summary>
    /// Creates a new resource using the default settings.
    /// </summary>
    /// <param name="humanName">Human friendly name for this resource.</param>
    /// <param name="maxJobs">Maximum number of simultaneous jobs being ran.</param>
    /// <param name="maxThroughput">Maximum throughput.</param>
    /// <param name="provider"></param>
    /// <remarks>
    /// Default settings limit max jobs to CPU threads and no throughput limit.
    /// </remarks>
    public Resource(string humanName, int maxJobs, TUnit maxThroughput, IDateTimeProvider? provider = null)
    {
        Name = humanName;
        MaxJobs = maxJobs == 0 ? Environment.ProcessorCount : maxJobs;
        MaxThroughput = maxThroughput;
        _semaphore = new SemaphoreSlim(MaxJobs);
        _channel = Channel.CreateBounded<PendingReport>(10);
        _tasks = new ConcurrentDictionary<ulong, Job<TResource, TUnit>>();
        _totalUsed = TUnit.AdditiveIdentity;
        provider ??= new DateTimeProvider();
        _provider = provider;
        _ = StartTask(CancellationToken.None);
    }

    /// <inheritdoc />
    public int MaxJobs { get; set; }

    /// <inheritdoc />
    public TUnit MaxThroughput { get; set; }

    /// <inheritdoc />
    public string Name { get; }

    // TODO: Optimize this by removing LINQ and calculating both counts at once. Iterating a dictionary's elements is already slow; doing it twice is oof. https://github.com/Nexus-Mods/NexusMods.App/issues/214

    /// <inheritdoc />
    public StatusReport<TUnit> StatusReport =>
        new(_tasks.Count(t => t.Value.Started),
            _tasks.Count(t => !t.Value.Started),
            _totalUsed);

    /// <inheritdoc />
    public async ValueTask<IJob<TResource, TUnit>> BeginAsync(string jobTitle, TUnit size, CancellationToken token)
    {
        var id = Interlocked.Increment(ref _nextId);
        var job = new Job<TResource, TUnit>
        {
            Id = id,
            Description = jobTitle,
            Size = size,
            Current = TUnit.AdditiveIdentity,
            TypedResource = this,
            StartedAt = _provider.GetCurrentTimeUtc(),
            CurrentAtResumeTime = TUnit.AdditiveIdentity,
            ResumedAt = _provider.GetCurrentTimeUtc()
        };

        _tasks.TryAdd(id, job);
        await _semaphore.WaitAsync(token);
        job.Started = true;
        return job;
    }

    /// <inheritdoc />
    public void ReportNoWait(Job<TResource, TUnit> job, TUnit processedSize)
    {
        job.Current += processedSize;
        IncrementTotalUsed(processedSize);
    }

    /// <inheritdoc />
    public void Finish(IJob<TResource, TUnit> job)
    {
        _semaphore.Release();
        _tasks.TryRemove(job.Id, out _);
    }

    /// <inheritdoc />
    public async ValueTask ReportAsync(Job<TResource, TUnit> job, TUnit size, CancellationToken token)
    {
        var tcs = new TaskCompletionSource();
        await _channel.Writer.WriteAsync(new PendingReport
        {
            Job = job,
            Size = size,
            Result = tcs
        }, token);
        await tcs.Task;
    }

    private async ValueTask StartTask(CancellationToken token)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(token))
        {
            IncrementTotalUsed(item.Size);
            if (MaxThroughput == TUnit.AdditiveIdentity)
            {
                item.Result.TrySetResult();
                continue;
            }

            var span = TimeSpan.FromSeconds(item.Size / MaxThroughput);
            await Task.Delay(span, token);
            item.Result.TrySetResult();
        }
    }

    private void IncrementTotalUsed(TUnit size)
    {
        lock (this)
            _totalUsed += size;
    }

    private struct PendingReport
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public Job<TResource, TUnit> Job { get; set; }
        public TUnit Size { get; init; }

        /// <summary>
        /// Result of the reported task. Once this is complete, calling thread
        /// which reported the task can resume.
        /// </summary>
        public TaskCompletionSource Result { get; init; }
    }
}
