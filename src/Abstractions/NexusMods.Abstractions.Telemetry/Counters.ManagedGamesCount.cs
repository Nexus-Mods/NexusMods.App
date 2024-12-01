using System.Diagnostics.Metrics;

namespace NexusMods.Abstractions.Telemetry;

public static partial class Counters
{
    public delegate int GetManagedGamesCountDelegate();

    /// <summary>
    /// Creates a counter for the number of managed games.
    /// </summary>
    public static void CreateManagedGamesCounter(
        this IMeterConfig meterConfig,
        GetManagedGamesCountDelegate getManagedGamesCountFunc)
    {
        GetMeter(meterConfig).CreateObservableUpDownCounter(
            name: InstrumentConstants.NameManagedGamesCount,
            observeValue: () => ObserveManagedGamesCount(getManagedGamesCountFunc)
        );
    }

    private static Measurement<int> ObserveManagedGamesCount(GetManagedGamesCountDelegate getManagedGamesCountFunc)
    {
        return new Measurement<int>(
            value: getManagedGamesCountFunc()
        );
    }
}
