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
    private bool _isFinished;
    private Percent _percentage;


    /// <summary>
    /// The unique identifier of the activity.
    /// </summary>
    public ActivityId Id { get; } = ActivityId.From(Guid.NewGuid());

    /// <inheritdoc />
    public void Dispose()
    {
        _isFinished = true;
        SendReport();
        monitor.Remove(this);
    }

    /// <inheritdoc />
    public void SetStatusMessage(string template, params object[] arguments)
    {
        _status = (template, arguments);
        SendReport();
    }

    /// <inheritdoc />
    public void SetProgress(Percent percent, CancellationToken token = bad)
    {
        _percentage = percent;
        SendReport();
    }

    /// <inheritdoc />
    public void AddProgress(Percent percent)
    {
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
            IsFinished = _isFinished,
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
