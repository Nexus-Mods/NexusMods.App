using Microsoft.Extensions.Logging;

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
        Message = "Operating System is `{os}` running `{framework}`"
    )]
    public static partial void RuntimeInformation(
        ILogger logger,
        string os,
        string framework
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
}
