using System.Diagnostics.Metrics;

namespace NexusMods.Abstractions.Telemetry;

public static partial class Counters
{
    /// <summary>
    /// Creates counter for the number of active users per version.
    /// </summary>
    public static void CreateActiveUsersPerVersionCounter(this IMeterConfig meterConfig, Version version)
    {
        var versionString = version.ToString(fieldCount: 3);
        GetMeter(meterConfig).CreateObservableUpDownCounter(
            name: InstrumentConstants.NameActiveUsers,
            observeValue: () => new Measurement<int>(
                value: 1,
                tags: new KeyValuePair<string, object?>(InstrumentConstants.TagVersion, versionString)
            )
        );
    }
}
