namespace NexusMods.Abstractions.DateTime;

/// <summary>
/// The default implementation of <see cref="IDateTimeProvider"/>.
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    /// <summary>
    /// A singleton instance of <see cref="DateTimeProvider"/>.
    /// </summary>
    public static DateTimeProvider Instance { get; } = new();

    /// <inheritdoc />
    public System.DateTime GetCurrentTimeUtc()
    {
        return System.DateTime.UtcNow;
    }
}
