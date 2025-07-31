using System.Numerics;
using System.Reactive.Subjects;
using System.Runtime.ExceptionServices;
using DynamicData.Kernel;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Jobs;

public sealed class JobContext<TJobDefinition, TJobResult> : IJobWithResult<TJobResult>, IJobContext<TJobDefinition> 
    where TJobDefinition : IJobDefinition<TJobResult> where TJobResult : notnull
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

    internal async Task Start()
    {
        SetStatus(JobStatus.Running);
        try
        {
            _result= await _action(this);
            SetStatus(JobStatus.Completed);
            _tcs.TrySetResult(_result.Value);
        }
        catch (OperationCanceledException)
        {
            SetStatus(JobStatus.Cancelled);
            _tcs.TrySetCanceled();
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

    public async Task YieldAsync()
    {
        // First check for cancellation (which includes force pause)
        JobCancellationToken.ThrowIfCancellationRequested();

        // Then check for cooperative 
        if (JobCancellationToken.IsPaused)
        {
            await PerformPause();

            // Check cancellation again after resume in case it was requested while paused
            JobCancellationToken.ThrowIfCancellationRequested();
        }
    }

    public JobCancellationToken JobCancellationToken => _jobCancellationToken;
    public CancellationToken CancellationToken => _jobCancellationToken.Token;
    
    public IJobMonitor Monitor { get; }
    public IJobGroup Group { get; }
    
    TJobDefinition IJobContext<TJobDefinition>.Definition => _definition;
    public IJobDefinition Definition => _definition;
    
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

    public async Task HandlePauseExceptionAsync(OperationCanceledException ex)
    {
        if (_jobCancellationToken.IsPausingCancellation(ex))
        {
            // This was a force pause, not true cancellation,
            // so wait until the job is resumed as usual.
            JobCancellationToken.RecycleToken();
            await PerformPause();
        }
        else
        {
            // This was real cancellation, re-throw preserving stack trace
            ExceptionDispatchInfo.Capture(ex).Throw();
        }
    }

    public Task WaitAsync(CancellationToken cancellationToken = default)
    {
        return _tcs.Task;
    }
    
    /// <summary>
    /// Try to get the exception that caused the job to fail.
    /// </summary>
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
    
    public void Cancel() => _jobCancellationToken.Cancel();
    public void Pause() => _jobCancellationToken.Pause();
    public void Resume() => _jobCancellationToken.Resume();

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

    private async Task PerformPause()
    {
        SetStatus(JobStatus.Paused);
        await JobCancellationToken.WaitForResumeAsync();
        SetStatus(JobStatus.Running);
    }
}
