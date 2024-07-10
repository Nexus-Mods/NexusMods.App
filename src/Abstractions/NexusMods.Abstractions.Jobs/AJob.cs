using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData.Kernel;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

[PublicAPI]
public abstract class AJob : IJob, IDisposable, IAsyncDisposable
{
    public JobId Id { get; }

    public IJobGroup? Group { get; }

    public JobStatus Status { get; }

    Progress IJob.Progress => Progress;
    internal MutableProgress Progress { get; }

    public IJobWorker? Worker { get; private set; }

    private readonly Subject<JobStatus> _subjectStatus;
    private readonly IConnectableObservable<JobStatus> _connectableObservableStatus;

    public IObservable<JobStatus> ObservableStatus => _connectableObservableStatus;

    private readonly CompositeDisposable _disposable = new();

    protected AJob(
        MutableProgress progress,
        IJobGroup? group = default,
        IJobWorker? worker = default)
    {
        Id = JobId.NewId();
        Status = JobStatus.None;

        Progress = progress;
        Group = group;
        Worker = worker;

        _subjectStatus = new Subject<JobStatus>();
        _connectableObservableStatus = _subjectStatus.Replay(bufferSize: 1);
        _disposable.Add(_connectableObservableStatus.Connect());
    }

    internal void SetStatus(JobStatus value)
    {
        if (Status.CanTransition(value)) _subjectStatus.OnNext(value);
        else throw new InvalidOperationException($"Transitioning from `{Status}` to `{value}` is invalid!");
    }

    internal void SetWorker(IJobWorker? value)
    {
        // TODO: sanity checks
        Worker = value;
    }

    public Task<JobResult> WaitToFinishAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task CancelAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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
            _subjectStatus.Dispose();
            _disposable.Dispose();
        }
    }

    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}
