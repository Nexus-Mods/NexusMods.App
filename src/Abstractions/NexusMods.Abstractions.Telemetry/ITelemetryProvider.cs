using System.Diagnostics.Metrics;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Telemetry;

[PublicAPI]
public interface ITelemetryProvider
{
    void ConfigureMetrics(Meter meter, IServiceProvider serviceProvider);
}
