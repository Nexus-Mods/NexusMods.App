using System.Numerics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData.Kernel;
using NexusMods.Abstractions.Activities;

namespace NexusMods.Activities;

/// <summary>
/// A concrete implementation of <see cref="IReadOnlyActivity"/>.
/// </summary>
/// <param name="monitor"></param>
/// <param name="group"></param>
/// <param name="payload"></param>
internal class Activity : IActivitySource, IReadOnlyActivity
{
    private readonly ActivityMonitor _monitor;

    //
    private readonly Subject<DateTime> _reports = new();

    /// <summary>
    /// The start time of the activity.
    /// </summary>
    protected DateTime StartTime { get; set; }

    /// <summary>
    /// The current status (text name) for the activity.
    /// </summary>
    protected (string Template, object[] Arguments) Status;

    /// <summary>
    /// The activity progress, stored as a double so we can handle clamping and overflow for rounding errors
    /// </summary>
    protected double Percentage;

    /// <summary>
    /// The starting value of the activity progress, required for resuming activities
    /// </summary>
    protected double StartingPercentage;

    /// <summary>
    /// The unique identifier of the activity.
    /// </summary>
    public ActivityId Id { get; }

    /// <summary>
    /// A user defined object that can be used to store additional information about the activity.
    /// </summary>
    public object? Payload { get; }

    public Activity(ActivityMonitor monitor, ActivityGroup group, object? payload)
    {
        Id = ActivityId.NewId();
        StartTime = DateTime.UtcNow;
        Payload = payload;
        Group = group;
        _monitor = monitor;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SendReport();
        _monitor.Remove(this);
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

    /// <inheritdoc />
    public void StartOrResume()
    {
        StartTime = DateTime.UtcNow;
        StartingPercentage = Percentage;

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
            CurrentProgress = Percent.CreateClamped(Percentage),
            StartingProgress = Percent.CreateClamped(StartingPercentage),
        };
    }

    private class TimeBox
    {
        public DateTime Time = DateTime.MinValue;
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
    public ActivityGroup Group { get; }
}

/// <summary>
/// A variant of <see cref="Activity"/> that allows you to specify a type for the progress value.
/// </summary>
/// <typeparam name="T"></typeparam>
internal class Activity<T>(ActivityMonitor monitor, ActivityGroup group, object? payload)
    : Activity(monitor, group, payload), IActivitySource<T>, IReadOnlyActivity<T>
    where T : struct, IDivisionOperators<T, T, double>, IAdditionOperators<T, T, T>, IDivisionOperators<T, double, T>,
    ISubtractionOperators<T, T, T>
{
    private Optional<T> _max;
    private Optional<T> _current;
    private Optional<T> _startingValue;

    /// <inheritdoc />
    public void StartOrResume(T startingValue)
    {
        _startingValue = startingValue;
        SetProgress(startingValue);
        StartOrResume();
    }

    /// <inheritdoc />
    public void SetMax(T max)
    {
        _max = max;
        SendReport();
    }

    /// <inheritdoc />
    public void SetProgress(T value)
    {
        _current = value;
        if (!_startingValue.HasValue)
            _startingValue = value;
        if (_max.HasValue) return;
        var percent = _max.Value / value;
        SetProgress(Percent.CreateClamped(percent));
    }

    /// <inheritdoc />
    public void AddProgress(T value)
    {
        if (!_current.HasValue)
        {
            _current = value;
        }
        else
        {
            _current = _current.Value + value;
        }

        if (!_max.HasValue) return;
        var percent = value / _max.Value;

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
        var reportTime = DateTime.UtcNow;
        var throughput = _current.HasValue && _startingValue.HasValue && reportTime > StartTime
            ? (_current.Value - _startingValue.Value) / (reportTime - StartTime).TotalSeconds
            : Optional<T>.None;

        var report = new ActivityReport<T>
        {
            ReportTime = reportTime,
            StartTime = StartTime,
            Id = Id,
            Status = Status,
            CurrentProgress = Percent.CreateClamped(Percentage),
            StartingProgress = Percent.CreateClamped(StartingPercentage),
            Max = _max,
            Current = _current,
            Throughput = throughput,
        };
        return report;
    }
}
