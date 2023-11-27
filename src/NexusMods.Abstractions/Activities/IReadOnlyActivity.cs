using NexusMods.DataModel.RateLimiting;

namespace NexusMods.DataModel.Activities;

/// <summary>
/// An activity is a long running operation that can be monitored by the user. This is the readonly interface for for
/// that activity, which can be used to display progress and status information to the user. This interface is just for
/// turning an activity into a stream of update events. We do this so that events can be throttled and have a maximum
/// update time. Some properties such as "Elapsed" will be updated on a regular basis, even if no activity has happened.
/// </summary>
public interface IReadOnlyActivity
{
    /// <summary>
    /// Gets an observable that will emit a report at least every <paramref name="maxInterval"/>, but no more often than
    /// minInterval. If activity happens, the report will be emitted immediately (throttled via the minInterval). Otherwise
    /// the maxInterval will be used to keep properties such as "Elapsed" up to date.
    ///
    /// The minimum interval defaults to 100 milliseconds, and the maximum interval defaults to 1 second.
    ///
    /// </summary>
    /// <returns></returns>
    public IObservable<ActivityReport> GetReports(TimeSpan? maxInterval, TimeSpan? minInterval);
}
