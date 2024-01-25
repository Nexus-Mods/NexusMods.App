using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.Paths;

namespace NexusMods.DataModel;

internal static partial class HighPerformanceLogging
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = "Processing jobs")]
    public static partial void ProcessingJobs(this ILogger logger);

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = "Done processing")]
    public static partial void DoneProcessing(this ILogger logger);

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = "Sending {bytes} byte message to queue {queue}")]
    public static partial void SendingByteMessageToQueue(
        this ILogger logger,
        Size bytes,
        string queue);

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = "Getting {id} of type {type}")]
    public static partial void GetIdOfType(
        this ILogger logger,
        IId id,
        string type);

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Debug,
        Message = "Waiting for write to {id} to complete")]
    public static partial void WaitingForWriteToId(
        this ILogger logger,
        IId id);

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = "Id {id} is updated")]
    public static partial void IdIsUpdated(
        this ILogger logger,
        IId id);
}
