using NexusMods.Abstractions.Activities;
using NexusMods.MnemonicDB.Abstractions;
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
    DownloaderState.Model PersistentState { get; }
    
    /// <summary>
    /// The download location of the task.
    /// </summary>
    public AbsolutePath DownloadLocation { get; }
    
    /// <summary>
    /// Calculates the download speed of the current job.
    /// </summary>
    /// <returns>Current speed in terms of bytes per second.</returns>
    Bandwidth Bandwidth { get; }
    
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

    /// <summary>
    /// Sets the <see cref="CompletedDownloadState.Hidden"/> on the task if it is completed.
    /// </summary>
    /// <param name="isHidden"> Value to set</param>
    /// <param name="tx">Transaction to use, if none is passed a new transaction is created and committed.</param>
    /// <remarks>If a transaction is passed, it is not committed, as it is assumed the caller will</remarks>
    /// <returns></returns>
    void SetIsHidden(bool isHidden, ITransaction tx);

    /// <summary>
    /// Reset (reload) the persistent state of the task from the database.
    /// </summary>
    /// <param name="db"></param>
    void ResetState(IDb db);
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
    Cancelled,
    
    /// <summary>
    /// The download is being extracted and analyzed.
    /// </summary>
    Analyzing,
}
