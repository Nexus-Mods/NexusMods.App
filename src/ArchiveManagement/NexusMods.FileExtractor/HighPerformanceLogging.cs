using Microsoft.Extensions.Logging;

namespace NexusMods.FileExtractor;

internal static partial class HighPerformanceLogging
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Debug,
        Message = "[7z.exe] {line}")]
    public static partial void ExecutingSevenZip(
        this ILogger logger,
        string line);
}
