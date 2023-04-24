using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.RateLimiting;
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
        Message = "Job {jobId} progress is {percent}")]
    public static partial void JobProgress(
        this ILogger logger,
        JobId jobId,
        Percent percent);

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = "Found new job {jobId}")]
    public static partial void NewJob(
        this ILogger logger,
        JobId jobId);

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = "Removing job {jobId}")]
    public static partial void RemovingJob(
        this ILogger logger,
        JobId jobId);

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
        Message = "Creating job {jobId} of type {jobType}")]
    public static partial void CreatingJob(
        this ILogger logger,
        JobId jobId,
        JobType jobType);

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = "Deleting job {jobId}")]
    public static partial void DeletingJob(
        this ILogger logger,
        JobId jobId);

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = "Updating job {jobId} progress to {percent}")]
    public static partial void UpdatingJobProgress(
        this ILogger logger,
        JobId jobId,
        Percent percent);
}
