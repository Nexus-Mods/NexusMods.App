namespace NexusMods.DataModel.RateLimiting;

/// <summary>
/// This interface can be used to provide the current time.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Retrieves the current time in UTC.
    /// </summary>
    DateTime GetCurrentTimeUtc();
}
