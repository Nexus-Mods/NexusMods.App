using System.Numerics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.Values;

namespace NexusMods.DataModel.Activities;

/// <summary>
/// A concrete implementation of <see cref="IActivity"/>.
/// </summary>
/// <param name="monitor"></param>
/// <param name="group"></param>
/// <param name="payload"></param>
public class Activity(ActivityMonitor monitor, ActivityGroup group, object? payload) : IActivitySource, IReadOnlyActivity
{
    //
    private readonly Subject<DateTime> _reports = new();

    /// <summary>
    /// The start time of the activity.
    /// </summary>
    protected readonly DateTime StartTime = DateTime.UtcNow;

    /// <summary>
    /// The current status (text name) for the activity.
    /// </summary>
    protected (string Template, object[] Arguments) Status;

    /// <summary>
    /// The activity progress, stored as a double so we can handle clamping and overflow for rounding errors
    /// </summary>
    protected double Percentage;

    /// <summary>
    /// The unique identifier of the activity.
    /// </summary>
    public ActivityId Id { get; } = ActivityId.From(Guid.NewGuid());

    /// <summary>
    /// A user defined object that can be used to store additional information about the activity.
    /// </summary>
    public object? Payload => payload;

    /// <inheritdoc />
    public void Dispose()
    {
        SendReport();
        monitor.Remove(this);
    }

    /// <inheritdoc />
    public void SetStatusMessage(string template, params object[] arguments)
    {
        Status = (template, arguments);
        SendReport();
    }

    /// <inheritdoc />
    public void SetProgress(Percent percent)
    {
        Percentage = percent.Value;
        SendReport();
    }

    /// <inheritdoc />
    public void AddProgress(Percent percent)
    {
        lock (this)
        {
            Percentage += percent.Value;
        }

        SendReport();
    }

    /// <summary>
    /// Sends a report to the monitor.
    /// </summary>
    protected void SendReport()
    {
        _reports.OnNext(DateTime.UtcNow);
    }

    protected virtual ActivityReport MakeReport()
    {
        return new ActivityReport
        {
            ReportTime = DateTime.UtcNow,
            StartTime = StartTime,
            Id = Id,
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
public class Activity<T>(ActivityMonitor monitor, ActivityGroup group, object? payload) :
    Activity(monitor, group, payload),
    IActivitySource<T>,
    IReadOnlyActivity<T>
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
    public void SetProgress(T value)
    {
        _current = value;
        if (_max is null) return;
        var percent = _max / value;
        SetProgress(Percent.CreateClamped(percent));
    }

    /// <inheritdoc />
    public void AddProgress(T value)
    {
        if (_current is null)
        {
            _current = value;
        }
        else
        {
            _current += value;
        }
        if (_max is null) return;
        var percent = value / _max;

        // Is NaN when value is 0 and max is 0
        if (double.IsNaN(percent))
        {
            percent = 0.0;
        }

        AddProgress(Percent.CreateClamped(percent));
    }

    /// <inheritdoc />
    protected override ActivityReport MakeReport()
    {
        return MakeTypedReport();
    }

    /// <inheritdoc />
    public ActivityReport<T> MakeTypedReport()
    {
        return new ActivityReport<T>
        {
            ReportTime = DateTime.UtcNow,
            StartTime = StartTime,
            Id = Id,
            Status = Status,
            CurrentProgress = Percent.CreateClamped(Percentage),
            Max = _max,
            Current = _current
        };
    }
}
