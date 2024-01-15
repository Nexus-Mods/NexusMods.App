using DynamicData;
using NexusMods.Abstractions.Values;
using NexusMods.DataModel;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Interfaces;

/// <summary>
/// This is a 'download manager' of sorts.
/// This service contains all of the downloads which have begun, or have already started.
///
/// This service only tracks the states and passes messages on behalf of currently live downloads.
/// </summary>
public interface IDownloadService : IDisposable
{
    /// <summary>
    /// Contains all downloads managed by the application.
    /// </summary>
    IObservable<IChangeSet<IDownloadTask>> Downloads { get; }

    /// <summary>
    /// This gets fired whenever a download task is started.
    /// </summary>
    IObservable<IDownloadTask> StartedTasks { get; }

    /// <summary>
    /// This gets fired whenever a status of download task is completed.
    /// This happens when <see cref="JobState.Finished"/> is true and <see cref="AnalyzedArchives"/> callback has completed.
    /// </summary>
    IObservable<IDownloadTask> CompletedTasks { get; }

    /// <summary>
    /// This gets fired whenever a status of download task is completed.
    /// This happens when <see cref="JobState.Finished"/> is true.
    /// </summary>
    IObservable<IDownloadTask> CancelledTasks { get; }

    /// <summary>
    /// This gets fired whenever a download task is paused.
    /// This happens when <see cref="JobState.Paused"/> is true.
    /// </summary>
    IObservable<IDownloadTask> PausedTasks { get; }

    /// <summary>
    /// This gets fired whenever a download task is resumed.
    /// This happens when <see cref="JobState.Running"/> is true after <see cref="JobState.Paused"/>.
    /// </summary>
    IObservable<IDownloadTask> ResumedTasks { get; }

    /// <summary>
    /// This gets fired whenever a download is complete and an archive has been analyzed.
    /// You can use this callback to gather additional metadata about the archive, or install the mods within.
    /// </summary>
    IObservable<(IDownloadTask task, DownloadId downloadId, string modName)> AnalyzedArchives { get; }

    /// <summary>
    /// Adds a task that will download from a NXM link.
    /// </summary>
    /// <param name="url">Url to download from.</param>
    Task AddNxmTask(NXMUrl url);

    /// <summary>
    /// Adds a task that will download from a HTTP link.
    /// </summary>
    /// <param name="url">Url to download from.</param>
    Task AddHttpTask(string url);

    /// <summary>
    /// Adds a task to the download queue.
    /// </summary>
    /// <param name="task">A task which has not yet been started.</param>
    Task AddTask(IDownloadTask task);

    /// <summary>
    /// This is a callback fired by individual implementations of <see cref="IDownloadTask"/>.
    /// Fires off the necessary events.
    /// </summary>
    void OnComplete(IDownloadTask task);

    /// <summary>
    /// This is a callback fired by individual implementations of <see cref="IDownloadTask"/>.
    /// Fires off the necessary events.
    /// </summary>
    void OnCancelled(IDownloadTask task);

    /// <summary>
    /// This is a callback fired by individual implementations of <see cref="IDownloadTask"/>.
    /// Fires off the necessary events.
    /// </summary>
    void OnPaused(IDownloadTask task);

    /// <summary>
    /// This is a callback fired by individual implementations of <see cref="IDownloadTask"/>.
    /// Fires off the necessary events.
    /// </summary>
    void OnResumed(IDownloadTask task);

    /// <summary>
    /// Gets the total throughput of all download operations in bytes per second.
    /// </summary>
    Size GetThroughput();

    /// <summary>
    /// Gets total progress percentage of all in progress download operations.
    /// Returns null if no downloads are in progress.
    /// </summary>
    Percent? GetTotalProgress();

    /// <summary>
    /// Updates the state of the task that's persisted behind the scenes.
    /// </summary>
    /// <param name="task">The task being finalized.</param>
    /// <remarks>
    ///    This should be called by the individual tasks right before they start downloading, such that the absolute
    ///    latest state is persisted in the case user kills the app.
    /// </remarks>
    void UpdatePersistedState(IDownloadTask task);

    /// <summary>
    /// Finishes the download process.
    /// </summary>
    /// <param name="task">The task being finalized.</param>
    /// <param name="tempPath">Path of the file to handle. Please delete this path at end of method.</param>
    /// <param name="modName">User friendly name under which this item is to be installed.</param>
    Task FinalizeDownloadAsync(IDownloadTask task, TemporaryPath tempPath, string modName);
}
