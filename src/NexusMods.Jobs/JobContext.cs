using System.Numerics;
using System.Reactive.Subjects;
using DynamicData.Kernel;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Jobs;

public sealed class JobContext<TJobDefinition, TJobResult> : IJobWithResult<TJobResult>, IJobContext<TJobDefinition> where TJobDefinition : IJobDefinition<TJobResult> where TJobResult : notnull
{
    private readonly BehaviorSubject<JobStatus> _status;
    private readonly Subject<Optional<Percent>> _progress;
    private readonly Subject<Optional<double>> _rateOfProgress;
    private readonly TJobDefinition _definition;
    private Optional<TJobResult> _result = Optional<TJobResult>.None;
    private readonly Func<IJobContext<TJobDefinition>, ValueTask<TJobResult>> _action;
    private readonly TaskCompletionSource<TJobResult> _tcs;
    private readonly JobCancellationToken _jobCancellationToken;

    internal JobContext(TJobDefinition definition, IJobMonitor monitor, IJobGroup jobGroup, JobCancellationToken jobCancellationToken, Func<IJobContext<TJobDefinition>, ValueTask<TJobResult>> action)
    {
        Id = JobId.NewId();
        Status = JobStatus.None;

        _tcs = new TaskCompletionSource<TJobResult>();
        _action = action;
        _definition = definition;
        Monitor = monitor;
        _status = new BehaviorSubject<JobStatus>(JobStatus.Created);
        _progress = new Subject<Optional<Percent>>();
        _rateOfProgress = new Subject<Optional<double>>();
        
        Progress = Optional<Percent>.None;
        RateOfProgress = Optional<double>.None;
        Group = jobGroup;
        _jobCancellationToken = jobCancellationToken;
    }

    public async Task Start()
    {
        // Just in case, as this API is publicly exposed
        if (Status == JobStatus.Running)
            return;
        
        // If paused, refuse to start until explicitly resumed
        if (JobCancellationToken.IsPaused)
        {
            SetStatus(JobStatus.Paused);
            return; // Don't auto-resume - require explicit Resume() call
        }
        
        SetStatus(JobStatus.Running);
        try
        {
            _result = await _action(this);
            SetStatus(JobStatus.Completed);
            _tcs.TrySetResult(_result.Value);
        }
        catch (OperationCanceledException)
        {
            // Distinguish between pause and true cancellation
            // Note: Job will restart when Start() is called again
            if (JobCancellationToken.IsPaused)
            {
                SetStatus(JobStatus.Paused);
                // Don't signal _tcs when paused - await should continue waiting
            }
            else
            {
                SetStatus(JobStatus.Cancelled);
                _tcs.TrySetCanceled();
            }
        }
        catch (Exception ex)
        {
            SetStatus(JobStatus.Failed);
            _tcs.TrySetException(ex);
        }
    }
    
    private void SetStatus(JobStatus status)
    {
        Status = status;
        _status.OnNext(status);
    }
    
    public JobId Id { get; }
    public JobStatus Status { get; set; }

    public IObservable<JobStatus> ObservableStatus => _status;
    public Optional<Percent> Progress { get; private set; }
    public IObservable<Optional<Percent>> ObservableProgress => _progress;
    public Optional<double> RateOfProgress { get; private set; }
    public IObservable<Optional<double>> ObservableRateOfProgress => _rateOfProgress;
    public bool CanBeCancelled => Status.IsActive();

    public Task YieldAsync()
    {
        // Simple cancellation check - pause becomes immediate cancellation
        JobCancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public JobCancellationToken JobCancellationToken => _jobCancellationToken;
    public CancellationToken CancellationToken => _jobCancellationToken.Token;
    
    public IJobMonitor Monitor { get; }
    public IJobGroup Group { get; }
    
    TJobDefinition IJobContext<TJobDefinition>.Definition => _definition;
    public IJobDefinition Definition => _definition;
    public Type JobType => typeof(TJobDefinition);
    
    public void SetPercent<TVal>(TVal current, TVal max) where TVal : IDivisionOperators<TVal, TVal, double>
    {
        var percent = Percent.CreateClamped(current / max);
        Progress = percent;
        _progress.OnNext(percent);
    }

    public void SetRateOfProgress(double rate)
    {
        RateOfProgress = rate;
        _rateOfProgress.OnNext(rate);
    }

    public void CancelAndThrow(string message)
    {
        Cancel();
        throw new OperationCanceledException(message);
    }


    public Task WaitAsync(CancellationToken cancellationToken = default)
    {
        return _tcs.Task;
    }
    
    /// <inheritdoc />
    public bool TryGetException(out Exception? exception)
    {
        if (_tcs.Task.IsFaulted)
        {
            exception = _tcs.Task.Exception;
            return true;
        }

        exception = null;
        return false;
    }

    public TJobResult Result => _result.HasValue ? _result.Value : throw new InvalidOperationException();
    
    public Task<TJobResult> WaitForResult(CancellationToken cancellationToken = default)
    {
        return _tcs.Task;
    }
    
    internal void Cancel() 
    {
        _jobCancellationToken.Cancel();
        
        // If the job was paused, we need to signal the TCS since the Start() method has already exited
        if (Status != JobStatus.Paused) return;
        SetStatus(JobStatus.Cancelled);
        _tcs.TrySetCanceled();
    }
    internal void Pause() => _jobCancellationToken.Pause();
    internal void Resume()
    {
        if (!_jobCancellationToken.IsPaused)
            return;

        // Clear pause flag only - JobMonitor handles restarting execution
        _jobCancellationToken.Resume();
    }

    public IJobContext AsContext() => this;
    
    void IJobContext.Cancel() => Cancel();
    void IJobContext.Pause() => Pause();
    void IJobContext.Resume() => Resume();

    public async ValueTask DisposeAsync()
    {
        if (_definition is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (_definition is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

}
