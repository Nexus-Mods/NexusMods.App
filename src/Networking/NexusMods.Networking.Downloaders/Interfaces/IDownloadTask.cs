using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Interfaces;

/// <summary>
/// Represents an individual task to download and install a mod.
/// </summary>
[Obsolete(message: "To be replaced with Jobs")]
public interface IDownloadTask
{
    
    /// <summary>
    /// Path of the ongoing download file
    /// </summary>
    public AbsolutePath DownloadPath { get; }
    
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
    /// Refresh (reload) the persistent state of the task from the database.
    /// </summary>
    /// <param name="db"></param>
    void RefreshState();
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
    /// The task has run to completion.
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
