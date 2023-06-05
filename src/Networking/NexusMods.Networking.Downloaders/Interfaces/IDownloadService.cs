using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Interfaces;

/// <summary>
/// This is a 'download manager' of sorts.
/// This service contains all of the downloads which have begun, or have already started.
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// Contains all downloads managed by the application.
    /// </summary>
    List<IDownloadTask> Downloads { get; }

    /// <summary>
    /// This gets fired whenever a download-and-install task is started.
    /// </summary>
    IObservable<IDownloadTask> StartedTasks { get; }

    /// <summary>
    /// This gets fired whenever a status of download-and-install task is completed.
    /// This happens when <see cref="JobState.Finished"/> is true.
    /// </summary>
    IObservable<IDownloadTask> CompletedTasks { get; }

    /// <summary>
    /// This gets fired whenever a status of download-and-install task is completed.
    /// This happens when <see cref="JobState.Finished"/> is true.
    /// </summary>
    IObservable<IDownloadTask> CancelledTasks { get; }

    /// <summary>
    /// This gets fired whenever a download-and-install task is paused.
    /// This happens when <see cref="JobState.Paused"/> is true.
    /// </summary>
    IObservable<IDownloadTask> PausedTasks { get; }

    /// <summary>
    /// This gets fired whenever a download-and-install task is resumed.
    /// This happens when <see cref="JobState.Running"/> is true after <see cref="JobState.Paused"/>.
    /// </summary>
    IObservable<IDownloadTask> ResumedTasks { get; }

    /// <summary>
    /// Adds a task that will download from a NXM link.
    /// </summary>
    /// <param name="url">Url to download from.</param>
    void AddNxmTask(NXMUrl url);

    /// <summary>
    /// Adds a task that will download from a HTTP link.
    /// </summary>
    /// <param name="url">Url to download from.</param>
    /// <param name="loadout">Loadout for the task.</param>
    void AddHttpTask(string url, Loadout loadout);

    /// <summary>
    /// Adds a task to the download queue.
    /// </summary>
    /// <param name="task">A task which has not yet been started.</param>
    void AddTask(IDownloadTask task);

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
}
