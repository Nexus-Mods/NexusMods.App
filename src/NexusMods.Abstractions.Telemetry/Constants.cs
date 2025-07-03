using JetBrains.Annotations;
using NexusMods.Sdk;

namespace NexusMods.Abstractions.Telemetry;

[PublicAPI]
internal static class Constants
{
    // NOTE(erri120): don't change this
    public const string ApplicationName = "NexusMods.App";

    public static string ServiceName => ApplicationName.ToLowerInvariant();
    public static string ServiceVersion => ApplicationConstants.Version.ToSafeString(maxFieldCount: 3);
    public static string MeterName => ServiceName;
}
