using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Telemetry;

namespace NexusMods.Telemetry;

/// <summary>
/// Tracking methods.
/// </summary>
[PublicAPI]
public static class Tracking
{
    internal static ITrackingDataSender? EventSender { get; set; }

    /// <summary>
    /// Check whether tracking is enabled to gate computational expensive operations.
    /// </summary>
    public static bool IsEnabled => EventSender is not null;

    /// <summary>
    /// Track an event.
    /// </summary>
    public static void AddEvent(EventDefinition definition, EventMetadata metadata)
    {
        EventSender?.AddEvent(definition, metadata);
    }
}

public static class TrackingRegistration
{
    public static IServiceCollection AddTracking(
        this IServiceCollection serviceCollection,
        TelemetrySettings settings)
    {
        if (!settings.IsEnabled) return serviceCollection;

        return serviceCollection
            .AddSingleton<ITrackingDataSender, TrackingDataSender>()
            .AddHostedService<TrackingService>();
    }
}
