using System.Diagnostics.Metrics;
using System.Globalization;

namespace NexusMods.Abstractions.Telemetry;

public static partial class Counters
{
    /// <summary>
    /// Creates a counter for the number of active users per language.
    /// </summary>
    public static void CreateUsersPerLanguageCounter(this IMeterConfig meterConfig)
    {
        GetMeter(meterConfig).CreateObservableUpDownCounter(
            name: InstrumentConstants.NameUsersPerLanguage,
            observeValue: ObserveLanguage
        );
    }

    private static CultureInfo GetCurrentLanguage() => Thread.CurrentThread.CurrentUICulture;

    private static readonly Measurement<int> LanguageMeasurement = new(
        value: 1,
        tags: new KeyValuePair<string, object?>(InstrumentConstants.TagLanguage, GetCurrentLanguage().Name)
    );

    private static Measurement<int> ObserveLanguage() => LanguageMeasurement;
}
