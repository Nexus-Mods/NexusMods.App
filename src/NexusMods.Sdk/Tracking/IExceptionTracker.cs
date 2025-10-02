using JetBrains.Annotations;

namespace NexusMods.Sdk.Tracking;

/// <summary>
/// Tracker for exceptions.
/// </summary>
[PublicAPI]
public interface IExceptionTracker
{
    /// <summary>
    /// Tracks an exception.
    /// </summary>
    void Track(Exception exception);
}
