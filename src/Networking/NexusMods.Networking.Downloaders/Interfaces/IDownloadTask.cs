using NexusMods.Abstractions.Activities;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Interfaces;

/// <summary>
/// Represents an individual task to download and install a mod.
/// </summary>
public interface IDownloadTask
{
    /// <summary>
    /// The DownloaderState of the task.
    /// </summary>
    DownloaderState.Model State { get; }
    
    /// <summary>
    /// Calculates the download speed of the current job.
    /// </summary>
    /// <returns>Current speed in terms of bytes per second.</returns>
    Bandwidth CalculateThroughput();
    
    /// <summary>
    /// The amount of data downloaded so far.
    /// </summary>
    Size Downloaded { get; }
    
    /// <summary>
    /// The percent completion of the task.
    /// </summary>
    Percent Progress { get; }
    
    /// <summary>
    /// Starts executing the task.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Cancels a download task, this is a one-way operation.
    /// </summary>
    Task Cancel();

    /// <summary>
    /// Pauses a download task, by saving the current state and temporarily cancelling it.
    /// </summary>
    Task Suspend();

    /// <summary>
    /// Resumes a download task.
    /// </summary>
    Task Resume();
}

/// <summary>
/// Current status of the task.
/// </summary>
public enum DownloadTaskStatus : byte
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
    Completed,
    
    /// <summary>
    /// The download was canceled.
    /// </summary>
    Cancelled
}
