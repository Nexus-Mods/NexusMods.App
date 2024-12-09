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
    internal static IEventSender? EventSender { get; set; }

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
            .AddSingleton<IEventSender, EventSender>()
            .AddHostedService<TrackingService>();
    }
}
