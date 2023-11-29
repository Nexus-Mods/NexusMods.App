using System.Numerics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.Values;

namespace NexusMods.DataModel.Activities;

public class Activity(ActivityMonitor monitor, ActivityGroup group, object? payload) : IActivitySource, IReadOnlyActivity
{
    private readonly Subject<DateTime> _reports = new();
    protected readonly DateTime _startTime = DateTime.UtcNow;
    protected (string Template, object[] Arguments) Status;
    /// <summary>
    /// The current run status of the activity.
    /// </summary>
    protected ActivityStatus RunStatus;

    /// <summary>
    /// The activity progress, stored as a double so we can handle clamping and overflow for rounding errors
    /// </summary>
    protected double Percentage;
    private TaskCompletionSource? _pauseTask = null;


    /// <summary>
    /// The unique identifier of the activity.
    /// </summary>
    public ActivityId Id { get; } = ActivityId.From(Guid.NewGuid());

    /// <summary>
    /// A user defined object that can be used to store additional information about the activity.
    /// </summary>
    public object? Payload => payload;

    /// <summary>
    /// Cancels the activity.
    /// </summary>
    public void Cancel()
    {
        lock (this)
        {
            if (RunStatus is ActivityStatus.Running or ActivityStatus.Paused)
            {
                _pauseTask?.TrySetCanceled();
                RunStatus = ActivityStatus.Cancelled;
            }
        }
        SendReport();
    }

    /// <inheritdoc />
    public void Pause()
    {
        lock (this)
        {
            if (RunStatus != ActivityStatus.Running)
            {
                throw new InvalidOperationException("Can only pause a running activity");
            }
            RunStatus = ActivityStatus.Paused;
            _pauseTask = new TaskCompletionSource();
        }
        SendReport();
    }

    /// <inheritdoc />
    public void Resume()
    {
        lock (this)
        {
            if (RunStatus != ActivityStatus.Paused)
            {
                throw new InvalidOperationException("Can only resume a paused activity");
            }
            RunStatus = ActivityStatus.Running;
            _pauseTask?.TrySetResult();
            _pauseTask = null;
        }
        SendReport();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (this)
        {
            if (RunStatus is ActivityStatus.Running or ActivityStatus.Paused)
            {
                _pauseTask?.TrySetCanceled();
                RunStatus = ActivityStatus.Finished;
            }
        }
        SendReport();
        monitor.Remove(this);
    }

    /// <inheritdoc />
    public void SetStatusMessage(string template, params object[] arguments)
    {
        Status = (template, arguments);
        SendReport();
    }
    private ValueTask MaybePause(CancellationToken token)
    {
        var val = _pauseTask?.Task;
        return val is null ? ValueTask.CompletedTask : new ValueTask(val);
    }

    /// <inheritdoc />
    public async ValueTask SetProgress(Percent percent, CancellationToken token)
    {
        await MaybePause(token);
        Percentage = percent.Value;
        SendReport();
    }

    /// <inheritdoc />
    public async ValueTask AddProgress(Percent percent, CancellationToken token)
    {
        await MaybePause(token);
        Percentage += percent.Value;
        SendReport();
    }

    /// <summary>
    /// Sends a report to the monitor.
    /// </summary>
    protected void SendReport()
    {
        _reports.OnNext(DateTime.UtcNow);
    }

    private ActivityReport MakeReport()
    {
        return new ActivityReport
        {
            ReportTime = DateTime.UtcNow,
            StartTime = _startTime,
            Id = Id,
            RunStatus = RunStatus,
            Status = Status,
            CurrentProgress = Percent.CreateClamped(Percentage)
        };
    }

    private class TimeBox
    {
        public DateTime Time  = DateTime.MinValue;
    }

    /// <inheritdoc />
    public IObservable<ActivityReport> GetReports(TimeSpan? maxInterval, TimeSpan? minInterval)
    {
        maxInterval ??= TimeSpan.FromSeconds(1);
        minInterval ??= TimeSpan.FromMilliseconds(100);

        var timeBox = new TimeBox();

        return Observable.Interval(maxInterval.Value)
            // Get the latest time every `maxInterval` seconds
            .Select(_ => DateTime.UtcNow)
            // Combine with the latest requests for reports
            .Merge(_reports)
            // Limit to minInterval
            .Where(x => x - timeBox.Time > minInterval.Value)
            // Generate the report
            .Select(_ => MakeReport())
            .StartWith(MakeReport())
            // Update the time box
            .Do(report => timeBox.Time = report.ReportTime);
    }

    /// <inheritdoc />
    public ActivityReport GetReport()
    {
        return MakeReport();
    }

    /// <inheritdoc />
    public ActivityGroup Group => group;
}

/// <summary>
/// A variant of <see cref="Activity"/> that allows you to specify a type for the progress value.
/// </summary>
/// <typeparam name="T"></typeparam>
public class Activity<T>(ActivityMonitor monitor, ActivityGroup group, object? payload) : Activity(monitor, group, payload), IActivitySource<T>, IReadOnlyActivity<T>
where T : IDivisionOperators<T, T, double>, IAdditionOperators<T, T, T>
{
    private T? _max;
    private T? _current;

    /// <inheritdoc />
    public void SetMax(T? max)
    {
        _max = max;
        SendReport();
    }

    /// <inheritdoc />
    public ValueTask SetProgress(T value, CancellationToken token)
    {
        _current = value;
        if (_max is null) return ValueTask.CompletedTask;
        var percent = _max / value;
        return SetProgress(Percent.CreateClamped(percent), token);
    }

    /// <inheritdoc />
    public ValueTask AddProgress(T value, CancellationToken token)
    {
        if (_current is null)
        {
            _current = value;
        }
        else
        {
            _current += value;
        }
        if (_max is null) return ValueTask.CompletedTask;
        var percent = value / _max;
        return AddProgress(Percent.CreateClamped(percent), token);
    }

    /// <inheritdoc />
    public ActivityReport<T> GetTypedReport()
    {
        return new ActivityReport<T>
        {
            ReportTime = DateTime.UtcNow,
            StartTime = _startTime,
            Id = Id,
            RunStatus = RunStatus,
            Status = Status,
            CurrentProgress = Percent.CreateClamped(Percentage),
            Max = _max,
            Current = _current
        };
    }
}
