using System.Diagnostics.Metrics;
using System.Runtime.InteropServices;

namespace NexusMods.Abstractions.Telemetry;

public static partial class Counters
{
    /// <summary>
    /// Creates a counter for the number of active users per operating system.
    /// </summary>
    public static void CreateUsersPerOSCounter(this IMeterConfig meterConfig)
    {
        GetMeter(meterConfig).CreateObservableUpDownCounter(
            name: InstrumentConstants.NameUsersPerOS,
            observeValue: ObserveOperatingSystem
        );
    }

    private static string GetOperatingSystemInformation()
    {
        // NOTE(erri120): Because Windows is just weird, there is no easy way
        // to extract the release number, eg: "Windows 10" or "Windows 11".
        // The best you can do is get the kernel version, which is super jank:
        // https://learn.microsoft.com/en-us/dotnet/api/system.environment.osversion
        // https://learn.microsoft.com/en-us/windows/win32/sysinfo/operating-system-version
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "Linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macOS";
        return "Unknown";
    }

    private static readonly Measurement<int> OperatingSystemMeasurement = new(
        value: 1,
        tags: new KeyValuePair<string, object?>(InstrumentConstants.TagOS, GetOperatingSystemInformation())
    );

    private static Measurement<int> ObserveOperatingSystem() => OperatingSystemMeasurement;
}
