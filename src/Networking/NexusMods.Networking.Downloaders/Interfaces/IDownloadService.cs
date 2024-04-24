using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.Abstractions.NexusWebApi.Types;

namespace NexusMods.Networking.Downloaders.Interfaces;

/// <summary>
/// This is a 'download manager' of sorts.
/// This service contains all the downloads which have begun, or have already started.
///
/// This service only tracks the states and passes messages on behalf of currently live downloads.
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// Contains all downloads managed by the application.
    /// </summary>
    ReadOnlyObservableCollection<IDownloadTask> Downloads { get; }
    
    /// <summary>
    /// Adds a task that will download from a NXM link.
    /// </summary>
    /// <param name="url">Url to download from.</param>
    Task<IDownloadTask> AddTask(NXMUrl url);

    /// <summary>
    /// Adds a task that will download from a HTTP link.
    /// </summary>
    /// <param name="url">Url to download from.</param>
    Task<IDownloadTask> AddTask(Uri url);
}
