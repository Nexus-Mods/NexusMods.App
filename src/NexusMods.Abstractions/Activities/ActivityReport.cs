using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.Values;

namespace NexusMods.DataModel.Activities;

/// <summary>
/// A report for an activity, these are immutable and emitted by the <see cref="IActivityMonitor"/>.
/// </summary>
public class ActivityReport
{
    public bool IsFinished { get; init; }

    /// <summary>
    /// The Unique identifier for the activity.
    /// </summary>
    public ActivityId Id { get; init; }

    /// <summary>
    /// The status of the activity.
    /// </summary>
    public (string Template, object[] Arguments) Status { get; init; }

    /// <summary>
    /// The start time of the activity.
    /// </summary>
    public required DateTime StartTime { get; init; }

    /// <summary>
    /// The timestamp of this specific report
    /// </summary>
    public required DateTime ReportTime { get; init; }

    /// <summary>
    /// The elapsed time since the activity started.
    /// </summary>
    public TimeSpan Elapsed => ReportTime - StartTime;

    /// <summary>
    /// The current progress of the activity, if any.
    /// </summary>
    public Percent? CurrentProgress { get; init; }

    /// <summary>
    /// The average progress per second.
    /// </summary>
    public Percent? PercentagePerSecond => CurrentProgress.HasValue && Elapsed.TotalSeconds > 0 ?
        Percent.CreateClamped(CurrentProgress.Value.Value / Elapsed.TotalSeconds) : null;

    /// <summary>
    /// The estimated total time for the activity, if any progress has been made.
    /// </summary>
    public TimeSpan? EstimatedTotalTime => PercentagePerSecond.HasValue ?
        TimeSpan.FromSeconds(100 / PercentagePerSecond.Value.Value) : null;

    /// <summary>
    /// The end time of the activity, if it can be calculated.
    /// </summary>
    public DateTime? EndTime => IsFinished ?
        StartTime + Elapsed : StartTime + EstimatedTotalTime;
}


/// <summary>
/// A typed report for an activity, these are immutable and emitted by the <see cref="IActivityMonitor"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ActivityReport<T> : ActivityReport
{
    /// <summary>
    /// The maximum value for the activity, if any.
    /// </summary>
    public T? Max { get; init; }

    /// <summary>
    /// The current value for the activity, if any.
    /// </summary>
    public T? Current { get; init; }

    /// <summary>
    /// If the activity has any progress this will return the amount of progress per second
    /// </summary>
    public T? Throughput { get; }
}
