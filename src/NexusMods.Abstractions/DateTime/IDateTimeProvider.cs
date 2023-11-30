namespace NexusMods.Abstractions.DateTime;

/// <summary>
/// A provider for the current time, useful for overriding in tests.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current time in UTC.
    /// </summary>
    /// <returns></returns>
    System.DateTime GetCurrentTimeUtc();
}
