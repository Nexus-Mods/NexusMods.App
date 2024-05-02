using System.Diagnostics.Metrics;
using JetBrains.Annotations;
using TelemetryConstants = NexusMods.Abstractions.Telemetry.Constants;

namespace NexusMods.Telemetry;

[PublicAPI]
internal static class Constants
{
    // NOTE(erri120): don't change this, this has to be in sync with the backend
    public static readonly TimeSpan ExportInterval = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan ExporterTimeout = TimeSpan.FromSeconds(10);

    public static readonly Uri MetricsEndpoint = new("https://collector.nexusmods.com/v1/metrics");
    public static readonly Uri TracesEndpoint = new("https://collector.nexusmods.com/v1/traces");

    public static readonly Meter Meter = new(name: TelemetryConstants.MeterName, version: TelemetryConstants.ServiceVersion);

    public static IEnumerable<KeyValuePair<string, object>> CreateAttributes()
    {
        // "conventional keys" taken from
        // https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.8.1/src/Shared/ResourceSemanticConventions.cs

        // NOTE(erri120): These attributes will appear on every metric, don't add more.
        yield return new KeyValuePair<string, object>("service.name", TelemetryConstants.ServiceName);
        yield return new KeyValuePair<string, object>("service.version", TelemetryConstants.ServiceVersion);
    }
}
