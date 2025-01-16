using System.Numerics;
using System.Reactive.Subjects;
using DynamicData.Kernel;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Jobs;

public sealed class JobContext<TJobDefinition, TJobResult> : IJobWithResult<TJobResult>, IJobContext<TJobDefinition> 
    where TJobDefinition : IJobDefinition<TJobResult> where TJobResult : notnull
{
    private readonly Subject<JobStatus> _status;
    private readonly Subject<Optional<Percent>> _progress;
    private readonly Subject<Optional<double>> _rateOfProgress;
    private readonly TJobDefinition _definition;
    private Optional<TJobResult> _result = Optional<TJobResult>.None;
    private readonly Func<IJobContext<TJobDefinition>, ValueTask<TJobResult>> _action;
    private readonly TaskCompletionSource<TJobResult> _tcs;

    internal JobContext(TJobDefinition definition, IJobMonitor monitor, IJobGroup jobGroup, Func<IJobContext<TJobDefinition>, ValueTask<TJobResult>> action)
    {
        Id = JobId.NewId();
        Status = JobStatus.None;

        _tcs = new TaskCompletionSource<TJobResult>();
        _action = action;
        _definition = definition;
        Monitor = monitor;
        _status = new Subject<JobStatus>();
        _progress = new Subject<Optional<Percent>>();
        _rateOfProgress = new Subject<Optional<double>>();
        
        Progress = Optional<Percent>.None;
        RateOfProgress = Optional<double>.None;
        Group = jobGroup;
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

    public Task YieldAsync()
    {
        return Task.CompletedTask;
    }

    public CancellationToken CancellationToken => Group.CancellationToken;
    
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
