using System.Collections.ObjectModel;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Library;

/// <summary>
/// Represents the library.
/// </summary>
[PublicAPI]
public interface ILibraryService
{
    /// <summary>
    /// Gets an observable collection containing all download activities.
    /// </summary>
    ReadOnlyObservableCollection<IDownloadActivity> DownloadActivities { get; }

    /// <summary>
    /// Enqueues a download.
    /// </summary>
    /// <remarks>
    /// This enqueues the download and optionally starts it, but this call is non-blocking and won't return
    /// after the download has finished.
    /// </remarks>
    /// <param name="downloadActivity">The download.</param>
    /// <param name="addPaused">Whether to add the download paused or start it immediately.</param>
    void EnqueueDownload(IDownloadActivity downloadActivity, bool addPaused = false);

    /// <summary>
    /// Adds a local file to the library.
    /// </summary>
    Task<Optional<LocalFile.ReadOnly>> AddLocalFileAsync(AbsolutePath absolutePath, CancellationToken cancellationToken = default);
}
