using JetBrains.Annotations;

namespace NexusMods.Telemetry;

/// <summary>
/// Interface for sending tracking data.
/// </summary>
[PublicAPI]
public interface ITrackingDataSender
{
    /// <summary>
    /// Adds an event.
    /// </summary>
    void AddEvent(EventDefinition definition, EventMetadata metadata);

    /// <summary>
    /// Adds an exception.
    /// </summary>
    void AddException(Exception exception);

    /// <summary>
    /// Runs the sender.
    /// </summary>
    ValueTask Run();
}
