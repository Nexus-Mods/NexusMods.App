using NexusMods.DataModel.RateLimiting;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Interfaces;

/// <summary>
/// Represents an individual task to download and install a mod.
/// </summary>
public interface IDownloadTask
{
    /// <summary>
    /// Gets all download jobs associated with this task for the purpose of calculating throughput.
    /// </summary>
    IEnumerable<IJob<Size>> DownloadJobs { get; }
    
    /// <summary>
    /// Service this task is associated with.
    /// </summary>
    DownloadService Owner { get; }
    
    /// <summary>
    /// Status of the current task.
    /// </summary>
    DownloadTaskStatus Status { get; }
    
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
    /// Pauses a download task.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes a download task.
    /// </summary>
    void Resume();
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
    /// The mod is being installed to a loadout.
    /// </summary>
    Installing,

    /// <summary>
    /// The task has ran to completion.
    /// </summary>
    Completed
}