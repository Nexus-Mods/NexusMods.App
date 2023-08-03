using NexusMods.DataModel.RateLimiting;
using NexusMods.Networking.Downloaders.Tasks.State;

namespace NexusMods.Networking.Downloaders.Interfaces;

/// <summary>
/// Represents an individual task to download and install a mod.
/// </summary>
public interface IDownloadTask
{
    /// <summary>
    /// Total size of items currently downloaded.
    /// </summary>
    /// <returns>0 if unknown.</returns>
    long DownloadedSizeBytes { get; }

    /// <summary>
    /// Calculates the download speed of the current job.
    /// </summary>
    /// <returns>Current speed in terms of bytes per second.</returns>
    long CalculateThroughput<TDateTimeProvider>(TDateTimeProvider provider) where TDateTimeProvider : IDateTimeProvider;

    /// <summary>
    /// Service this task is associated with.
    /// </summary>
    IDownloadService Owner { get; }

    /// <summary>
    /// Status of the current task.
    /// </summary>
    DownloadTaskStatus Status { get; set; }

    /// <summary>
    /// Friendly name for the task.
    /// </summary>
    /// <remarks>
    ///     Only available after download has started.
    /// </remarks>
    public string FriendlyName { get; }

    /// <summary>
    /// Starts executing the task.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Cancels a download task.
    /// </summary>
    void Cancel();

    /// <summary>
    /// Pauses a download task, by saving the current state and temporarily cancelling it.
    /// </summary>
    void Suspend();

    /// <summary>
    /// Resumes a download task.
    /// </summary>
    Task Resume();

    /// <summary>
    /// Exports state for performing a suspend operation.
    /// </summary>
    /// <remarks>Suspend means 'pause download by terminating it, leaving partial download intact'.</remarks>
    DownloaderState ExportState();
}

// TODO: These statuses need unit tests for individual downloaders.

/// <summary>
/// Current status of the task.
/// </summary>
public enum DownloadTaskStatus
{
    /// <summary>
    /// The task is not yet initialized.
    /// </summary>
    Idle,

    /// <summary>
    /// The download was paused.
    /// </summary>
    Paused,

    /// <summary>
    /// The mod is currently being downloaded.
    /// </summary>
    Downloading,

    /// <summary>
    /// The mod is being archived (and possibly installed) to a loadout.
    /// </summary>
    Installing,

    /// <summary>
    /// The task has ran to completion.
    /// </summary>
    Completed
}
