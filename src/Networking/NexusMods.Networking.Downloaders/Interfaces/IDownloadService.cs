using DynamicData;
using NexusMods.DataModel;
using NexusMods.Hashing.xxHash64;
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
