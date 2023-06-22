namespace NexusMods.DataModel.RateLimiting;

/// <inheritdoc />
public class DateTimeProvider : IDateTimeProvider
{
    /// <summary>
    /// Shared instance of this class.
    /// </summary>
    public static DateTimeProvider Instance { get; } = new();
    
    /// <inheritdoc />
    public DateTime GetCurrentTimeUtc() => DateTime.UtcNow;
}
