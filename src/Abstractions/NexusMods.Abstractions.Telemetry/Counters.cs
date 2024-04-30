using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Telemetry;

[PublicAPI]
public interface IMeterConfig;

internal class MeterConfig : IMeterConfig
{
    public readonly Meter Meter;
    public MeterConfig(Meter meter)
    {
        Meter = meter;
    }
}

[PublicAPI]
public static partial class Counters
{
    private static HashSet<string> _called = new(comparer: StringComparer.OrdinalIgnoreCase);
    private static void AssertSingleCall([CallerMemberName] string caller = "")
    {
        if (_called.Add(caller)) return;
        throw new NotSupportedException($"Method {caller} can't be called twice!");
    }

    private static MeterConfig GetImpl(IMeterConfig fake, [CallerMemberName] string caller = "")
    {
        if (fake is not MeterConfig impl) throw new NotSupportedException($"Invalid meter config: {fake}");
        AssertSingleCall(caller: caller);
        return impl;
    }

    private static Meter GetMeter(IMeterConfig fake, [CallerMemberName] string caller = "")
    {
        return GetImpl(fake, caller: caller).Meter;
    }
}
