using System.Reactive.Linq;
using System.Reactive.Subjects;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.Values;

namespace NexusMods.DataModel.Activities;

public class Activity(ActivityMonitor monitor, ActivityGroup group) : IActivitySource, IReadOnlyActivity
{
    private readonly Subject<DateTime> _reports = new();
    private readonly DateTime _startTime = DateTime.UtcNow;
    private (string Template, object[] Arguments) _status;
    private ActivityStatus _runStatus;
    private Percent _percentage;
    private TaskCompletionSource? _pauseTask = null;


    /// <summary>
    /// The unique identifier of the activity.
    /// </summary>
    public ActivityId Id { get; } = ActivityId.From(Guid.NewGuid());

    /// <summary>
    /// Cancels the activity.
    /// </summary>
    public void Cancel()
    {
        lock (this)
        {
            if (_runStatus is ActivityStatus.Running or ActivityStatus.Paused)
            {
                _pauseTask?.TrySetCanceled();
                _runStatus = ActivityStatus.Cancelled;
            }
        }
        SendReport();
    }

    /// <inheritdoc />
    public void Pause()
    {
        lock (this)
        {
            if (_runStatus != ActivityStatus.Running)
            {
                throw new InvalidOperationException("Can only pause a running activity");
            }
            _runStatus = ActivityStatus.Paused;
            _pauseTask = new TaskCompletionSource();
        }
        SendReport();
    }

    /// <inheritdoc />
    public void Resume()
    {
        lock (this)
        {
            if (_runStatus != ActivityStatus.Paused)
            {
                throw new InvalidOperationException("Can only resume a paused activity");
            }
            _runStatus = ActivityStatus.Running;
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
            if (_runStatus is ActivityStatus.Running or ActivityStatus.Paused)
            {
                _pauseTask?.TrySetCanceled();
                _runStatus = ActivityStatus.Finished;
            }
        }
        SendReport();
        monitor.Remove(this);
    }

    /// <inheritdoc />
    public void SetStatusMessage(string template, params object[] arguments)
    {
        _status = (template, arguments);
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
        _percentage = percent;
        SendReport();
    }

    /// <inheritdoc />
    public async ValueTask AddProgress(Percent percent, CancellationToken token)
    {
        await MaybePause(token);
        _percentage += percent;
        SendReport();
    }

    private void SendReport()
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
            RunStatus = _runStatus,
            Status = _status,
            CurrentProgress = _percentage
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
    public ActivityGroup Group => group;
}
