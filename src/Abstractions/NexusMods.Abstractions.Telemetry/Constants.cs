using JetBrains.Annotations;
using NexusMods.App.BuildInfo;

namespace NexusMods.Abstractions.Telemetry;

[PublicAPI]
internal static class Constants
{
    // NOTE(erri120): don't change this
    public const string ApplicationName = "NexusMods.App";
    public static Version ApplicationVersion => ApplicationConstants.Version ?? new Version(0, 0, 0);

    public static string ServiceName => ApplicationName.ToLowerInvariant();
    public static string ServiceVersion => ApplicationVersion.ToString(fieldCount: 3);
    public static string MeterName => ServiceName;
}
