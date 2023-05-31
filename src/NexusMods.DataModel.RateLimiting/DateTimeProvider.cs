namespace NexusMods.DataModel.RateLimiting;

/// <inheritdoc />
public class DateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTime GetCurrentTimeUtc() => DateTime.UtcNow;
}
