using System.Collections.ObjectModel;
using DynamicData.Kernel;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Interfaces;

/// <summary>
/// This is a 'download manager' of sorts.
/// This service contains all the downloads which have begun, or have already started.
///
/// This service only tracks the states and passes messages on behalf of currently live downloads.
/// </summary>
[Obsolete(message: "To be replaced with ILibraryService")]
public interface IDownloadService
{
    /// <summary>
    /// Contains all downloads managed by the application.
    /// </summary>
    ReadOnlyObservableCollection<IDownloadTask> Downloads { get; }
    
    /// <summary>
    /// The base directory for ongoing downloads
    /// </summary>
    AbsolutePath OngoingDownloadsDirectory { get; }
    
    /// <summary>
    /// Adds a task that will download from a NXM link.
    /// </summary>
    /// <param name="url">Url to download from.</param>
    Task<IDownloadTask> AddTask(NXMModUrl url);

    /// <summary>
    /// Adds a task that will download from a HTTP link.
    /// </summary>
    /// <param name="url">Url to download from.</param>
    Task<IDownloadTask> AddTask(Uri url);
    
    /// <summary>
    /// Gets the total throughput of all downloads.
    /// </summary>
    Size GetThroughput();
    
    /// <summary>
    /// Gets the total progress of all downloads.
    /// </summary>
    Optional<Percent> GetTotalProgress();

    /// <summary>
    /// Sets the <see cref="CompletedDownloadState.Hidden"/> on the tasks if they are completed.
    /// </summary>
    Task SetIsHidden(bool isHidden, IDownloadTask[] targets);
}
