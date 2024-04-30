namespace NexusMods.Abstractions.Telemetry;

public static partial class Counters
{
    /// <summary>
    /// Creates counter for the number of active users.
    /// </summary>
    public static void CreateActiveUsersCounter(this IMeterConfig meterConfig)
    {
        GetMeter(meterConfig).CreateObservableUpDownCounter(
            name: InstrumentConstants.NameActiveUsers,
            observeValue: static () => 1
        );
    }
}
