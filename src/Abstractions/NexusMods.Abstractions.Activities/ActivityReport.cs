using System.Numerics;
using DynamicData.Kernel;

namespace NexusMods.Abstractions.Activities;

/// <summary>
/// A report for an activity, these are immutable and emitted by the <see cref="IActivityMonitor"/>.
/// </summary>
public class ActivityReport
{
    /// <summary>
    /// True if execution of this activity has concluded, else false.
    /// </summary>
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
    public Optional<Percent> PercentagePerSecond => CurrentProgress.HasValue && Elapsed.TotalSeconds > 0 ?
        Percent.CreateClamped(CurrentProgress.Value.Value / Elapsed.TotalSeconds) : Optional<Percent>.None;

    /// <summary>
    /// The estimated total time for the activity, if any progress has been made.
    /// </summary>
    public Optional<TimeSpan> EstimatedTotalTime => PercentagePerSecond.HasValue ?
        Optional.Some(TimeSpan.FromSeconds(100 / PercentagePerSecond.Value.Value)) : Optional<TimeSpan>.None;

    /// <summary>
    /// The end time of the activity, if it can be calculated.
    /// </summary>
    public Optional<DateTime> EndTime => IsFinished ?
        StartTime + Elapsed : StartTime + EstimatedTotalTime.Value;
}


/// <summary>
/// A typed report for an activity, these are immutable and emitted by the <see cref="IActivityMonitor"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ActivityReport<T> : ActivityReport
where T : struct, IDivisionOperators<T, double, T>
{
    /// <summary>
    /// The maximum value for the activity, if any.
    /// </summary>
    public Optional<T> Max { get; init; }

    /// <summary>
    /// The current value for the activity, if any.
    /// </summary>
    public Optional<T> Current { get; init; }

    /// <summary>
    /// If the activity has any progress this will return the amount of progress per second
    /// </summary>
    public Optional<T> Throughput => Current != null && Elapsed.TotalSeconds > 0 ?
        Optional.Some(Current.Value / Elapsed.TotalSeconds) : default;
}
