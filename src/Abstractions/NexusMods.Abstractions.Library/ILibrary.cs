using System.Collections.ObjectModel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Library;

/// <summary>
/// Represents the library.
/// </summary>
[PublicAPI]
public interface ILibrary
{
    /// <summary>
    /// Gets an observable collection containing all download activities.
    /// </summary>
    ReadOnlyObservableCollection<IDownloadActivity> DownloadActivities { get; }

    /// <summary>
    /// Adds a download to the library.
    /// </summary>
    /// <param name="downloadActivity">The download.</param>
    /// <param name="addPaused">Whether to add the download paused or start it immediately.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddDownloadAsync(IDownloadActivity downloadActivity, bool addPaused = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a local file to the library.
    /// </summary>
    Task<LocalFile.ReadOnly> AddLocalFileAsync(AbsolutePath absolutePath, CancellationToken cancellationToken = default);
}
