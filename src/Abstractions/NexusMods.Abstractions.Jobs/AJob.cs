using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

[PublicAPI]
public abstract class AJob : IJobGroup, IDisposable, IAsyncDisposable
{
    public JobId Id { get; }
    public IJobGroup? Group { get; }
    public JobStatus Status { get; private set; }
    public IJobWorker? Worker { get; private set; }
    public JobResult? Result { get; private set; }

    Progress IJob.Progress => Progress;
    internal MutableProgress Progress { get; }

    private readonly List<IJob> _collection;
    private readonly ObservableCollection<IJob> _observableCollection;
    public ReadOnlyObservableCollection<IJob> ObservableCollection { get; }

    private readonly Subject<JobStatus> _subjectStatus;
    private readonly IConnectableObservable<JobStatus> _connectableObservableStatus;
    public IObservable<JobStatus> ObservableStatus => _connectableObservableStatus;

    private readonly Subject<JobResult> _subjectResult;
    private readonly IConnectableObservable<JobResult> _connectableObservableResult;
    public IObservable<JobResult> ObservableResult => _connectableObservableResult;

    internal CancellationTokenSource CancellationTokenSource { get; } = new();
    internal Task? Task { get; set; }
    internal bool IsRequestingPause { get; set; }

    private readonly CompositeDisposable _disposable = new();

    protected AJob(
        MutableProgress progress,
        IJobGroup? group = default,
        IJobWorker? worker = default,
        IJobMonitor? monitor = default)
    {
        Id = JobId.NewId();
        Status = JobStatus.None;

        Progress = progress;
        Group = group;
        Worker = worker;

        _subjectStatus = new Subject<JobStatus>();
        _connectableObservableStatus = _subjectStatus.Replay(bufferSize: 1);
        _disposable.Add(_subjectStatus);
        _disposable.Add(_connectableObservableStatus.Connect());

        _subjectResult = new Subject<JobResult>();
        _connectableObservableResult = _subjectResult.Replay(bufferSize: 1);
        _disposable.Add(_subjectResult);
        _disposable.Add(_connectableObservableResult.Connect());

        _collection = [];
        _observableCollection = new ObservableCollection<IJob>(_collection);
        ObservableCollection = new ReadOnlyObservableCollection<IJob>(_observableCollection);

        monitor?.RegisterJob(this);
    }

    public IEnumerator<IJob> GetEnumerator() => _collection.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();
    public int Count => _observableCollection.Count;
    public IJob this[int index] => _collection[index];

    internal void AddJob(AJob job)
    {
        // TODO: sanity checks and other stuff
        _observableCollection.Add(job);
    }

    private static MutableProgress CreateGroupProgress()
    {
        // TODO: figure out what to use here
        throw new NotImplementedException();
    }

    internal void SetStatus(JobStatus value)
    {
        Status.AssertTransition(value);
        Status = value;
        _subjectStatus.OnNext(value);
    }

    internal void SetWorker(IJobWorker? value)
    {
        // TODO: sanity checks
        Worker = value;
    }

    internal void SetResult(JobResult value, bool inferStatus)
    {
        // TODO: sanity checks
        Result = value;
        _subjectResult.OnNext(value);

        if (!inferStatus) return;
        if (value.TryGetCompleted(out _))
        {
            SetStatus(JobStatus.Completed);
        } else if (value.TryGetCancelled(out _))
        {
            SetStatus(JobStatus.Cancelled);
        } else if (value.TryGetFailed(out _))
        {
            SetStatus(JobStatus.Failed);
        }
    }

    public async Task<JobResult> WaitToFinishAsync(CancellationToken cancellationToken = default)
    {
        if (Result is not null) return Result;
        var result = await ObservableResult
            .FirstAsync()
            .ToTask(cancellationToken);

        return result;
    }

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (Worker is null) throw new InvalidOperationException("Worker is null, unable to start job");
        return Worker.StartAsync(job: this, cancellationToken: cancellationToken);
    }

    public ValueTask PauseAsync(CancellationToken cancellationToken = default)
    {
        if (Worker is null) throw new InvalidOperationException("Worker is null, unable to pause job");
        return Worker.PauseAsync(job: this, cancellationToken: cancellationToken);
    }

    public ValueTask CancelAsync(CancellationToken cancellationToken = default)
    {
        if (Worker is null) throw new InvalidOperationException("Worker is null, unable to cancel job");
        return Worker.CancelAsync(job: this, cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposable.Dispose();
        }
    }

    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}
