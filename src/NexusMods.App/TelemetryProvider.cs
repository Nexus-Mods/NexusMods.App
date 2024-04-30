using NexusMods.Abstractions.Telemetry;

namespace NexusMods.App;

internal class TelemetryProvider : ITelemetryProvider
{
    public void ConfigureMetrics(IMeterConfig meterConfig, IServiceProvider serviceProvider)
    {
        meterConfig.CreateActiveUsersCounter();
        meterConfig.CreateUsersPerOSCounter();
        meterConfig.CreateUsersPerLanguageCounter();
    }
}
