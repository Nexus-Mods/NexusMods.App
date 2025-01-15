using Microsoft.Extensions.Logging;

namespace NexusMods.App.UI;

internal static partial class HighPerformanceLogging
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = "Finding View for {viewModel}")]
    public static partial void FindingView(
        this ILogger logger,
        string viewModel);
}
