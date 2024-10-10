using System.Diagnostics.Metrics;
using NexusMods.Abstractions.NexusWebApi.Types.V2;

namespace NexusMods.Abstractions.Telemetry;

public static partial class Counters
{
    public record struct LoadoutModCount(string GameName, int Count);
    public delegate LoadoutModCount[] GetModsPerLoadoutDelegate();

    /// <summary>
    /// Creates a counter for the number of mods per game.
    /// </summary>
    public static void CreateModsPerGameCounter(
        this IMeterConfig meterConfig,
        GetModsPerLoadoutDelegate func)
    {
        GetMeter(meterConfig).CreateObservableUpDownCounter(
            name: InstrumentConstants.NameModsPerGame,
            observeValues: () => ObserveModsPerGame(func)
        );
    }

    private static double AggregateModCount(IGrouping<string, LoadoutModCount> grouping)
    {
        // NOTE(erri120): Using average as the aggregation method to go from
        // multiple loadouts with mod counts to a single mod count.
        return grouping.Average(x => x.Count);
    }

    private static Measurement<double>[] ObserveModsPerGame(GetModsPerLoadoutDelegate func)
    {
        var values = func();
        var measurements = values
            .GroupBy(x => x.GameName)
            .Select(grouping =>
            {
                var game = grouping.Key;
                var value = AggregateModCount(grouping);
                return new Measurement<double>(
                    value: value,
                    tags: game.ToTag()
                );
            })
            .ToArray();

        return measurements;
    }
}
