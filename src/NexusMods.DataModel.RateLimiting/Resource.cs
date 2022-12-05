using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Channels;

namespace NexusMods.DataModel.RateLimiting;

public class Resource<TResource, TUnit> : IResource<TResource, TUnit> 
    where TUnit : IAdditionOperators<TUnit, TUnit, TUnit>, 
    IAdditiveIdentity<TUnit, TUnit>, IDivisionOperators<TUnit, TUnit, double>,
    IEqualityOperators<TUnit, TUnit, bool>
{
    private Channel<PendingReport> _channel;
    private SemaphoreSlim _semaphore;
    private readonly ConcurrentDictionary<ulong, Job<TResource, TUnit>> _tasks;
    private ulong _nextId;
    private TUnit _totalUsed;
    public IEnumerable<IJob> Jobs => _tasks.Values;

    public Resource(string humanName) : this(humanName, 0, TUnit.AdditiveIdentity)
    {
        
    }

    public Resource(string humanName, int maxJobs, TUnit maxThroughput)
    {
        Name = humanName;
        MaxJobs = maxJobs == 0 ? Environment.ProcessorCount : maxJobs;
        MaxThroughput = maxThroughput;
        _semaphore = new SemaphoreSlim(MaxJobs);
        _channel = Channel.CreateBounded<PendingReport>(10);
        _tasks = new ConcurrentDictionary<ulong, Job<TResource, TUnit>>();
        _totalUsed = TUnit.AdditiveIdentity;
        
        var tsk = StartTask(CancellationToken.None);
    }

    public Resource(string humanName, Func<Task<(int MaxTasks, TUnit MaxThroughput)>> settingGetter)
    {
        Name = humanName;
        _tasks = new ConcurrentDictionary<ulong, Job<TResource, TUnit>>();
        
        Task.Run(async () =>
        {
            var (maxJobs, maxThroughput) = await settingGetter();
            MaxJobs = maxJobs;
            MaxThroughput = maxThroughput;
            _semaphore = new SemaphoreSlim(MaxJobs);
            _channel = Channel.CreateBounded<PendingReport>(10);
            
            await StartTask(CancellationToken.None);
        });
    }

    public int MaxJobs { get; set; }
    public TUnit MaxThroughput { get; set; }
    public string Name { get; }

    public async ValueTask<IJob<TResource, TUnit>> Begin(string jobTitle, TUnit size, CancellationToken token)
    {
        var id = Interlocked.Increment(ref _nextId);
        var job = new Job<TResource, TUnit>
        {
            Id = id,
            Description = jobTitle,
            Size = size,
            Resource = this
        };
        _tasks.TryAdd(id, job);
        await _semaphore.WaitAsync(token);
        job.Started = true;
        return job;
    }

    public void ReportNoWait(Job<TResource, TUnit> job, TUnit processedSize)
    {
        job.Current += processedSize;
        lock (this)
        {
            _totalUsed += processedSize;
        }
    }

    public void Finish(IJob<TResource, TUnit> job)
    {
        _semaphore.Release();
        _tasks.TryRemove(job.Id, out _);
    }

    public async ValueTask Report(Job<TResource, TUnit> job, TUnit size, CancellationToken token)
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

    public StatusReport<TUnit> StatusReport =>
        new(_tasks.Count(t => t.Value.Started),
            _tasks.Count(t => !t.Value.Started),
            _totalUsed);

    private async ValueTask StartTask(CancellationToken token)
    {
        var sw = new Stopwatch();
        sw.Start();

        await foreach (var item in _channel.Reader.ReadAllAsync(token))
        {
            lock (this)
            {
                _totalUsed += item.Size;
            }
            if (MaxThroughput == TUnit.AdditiveIdentity)
            {
                item.Result.TrySetResult();
                sw.Restart();
                continue;
            }

            var span = TimeSpan.FromSeconds(item.Size / MaxThroughput);


            await Task.Delay(span, token);

            sw.Restart();

            item.Result.TrySetResult();
        }
    }

    private struct PendingReport
    {
        public Job<TResource, TUnit> Job { get; set; }
        public TUnit Size { get; init; }
        public TaskCompletionSource Result { get; init; }
    }
}