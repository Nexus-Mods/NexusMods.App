using Microsoft.Extensions.Logging;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI.Pages.CollectionDownload;

namespace NexusMods.App;

internal static partial class LogMessages
{
    [LoggerMessage(
        EventId = 0, EventName = nameof(StartingProcess),
        Level = LogLevel.Information,
        Message = "Main process started as `{pid}` - `{executable}` `{args}`"
    )]
    public static partial void StartingProcess(
        ILogger logger,
        string? executable,
        int pid,
        string[] args
    );

    [LoggerMessage(
        EventId = 1, EventName = nameof(RuntimeInformation),
        Level = LogLevel.Information,
        Message = "Operating System is `{os}` running `{framework}` with installation method `{installationMethod}`"
    )]
    public static partial void RuntimeInformation(
        ILogger logger,
        string os,
        string framework,
        InstallationMethod installationMethod
    );

    [LoggerMessage(
        EventId = 2, EventName = nameof(UnobservedTaskException),
        Level = LogLevel.Error,
        Message = "Encountered an unobserved task exception in the Task Scheduler from sender of type `{senderType}`: `{sender}`"
    )]
    public static partial void UnobservedTaskException(
        ILogger logger,
        Exception e,
        object? sender,
        Type? senderType
    );

    [LoggerMessage(
        EventId = 3, EventName = nameof(UnobservedReactiveThrownException),
        Level = LogLevel.Error,
        Message = "Encountered an exception published to an object with an unobserved ThrownExceptions property"
    )]
    public static partial void UnobservedReactiveThrownException(
        ILogger logger,
        Exception e
    );

    [LoggerMessage(
        EventId = 4, EventName = nameof(R3UnhandledException),
        Level = LogLevel.Error,
        Message = "Encountered an unhandled exception in R3"
    )]
    public static partial void R3UnhandledException(
        ILogger logger,
        Exception e
    );
}
