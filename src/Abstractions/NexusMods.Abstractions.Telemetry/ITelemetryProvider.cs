using JetBrains.Annotations;

namespace NexusMods.Abstractions.Telemetry;

[PublicAPI]
public interface ITelemetryProvider
{
    void ConfigureMetrics(IMeterConfig meterConfig);
}
