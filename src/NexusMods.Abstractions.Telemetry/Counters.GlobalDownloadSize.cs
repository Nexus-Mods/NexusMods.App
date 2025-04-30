using System.Diagnostics.Metrics;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Telemetry;

public static partial class Counters
{
    public delegate Size GetDownloadSizeDelegate();

    public static void CreateGlobalDownloadSizeCounter(
        this IMeterConfig meterConfig,
        GetDownloadSizeDelegate func)
    {
        GetMeter(meterConfig).CreateObservableUpDownCounter(
            name: InstrumentConstants.NameGlobalDownloadSize,
            observeValue: () => ObserveDownloadSize(func)
        );
    }

    private static Measurement<double> ObserveDownloadSize(GetDownloadSizeDelegate func)
    {
        var value = func();
        return new Measurement<double>(
            value: value.Value
        );
    }
}
