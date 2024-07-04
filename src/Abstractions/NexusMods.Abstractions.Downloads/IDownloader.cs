using JetBrains.Annotations;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Represents a downloader.
/// </summary>
[PublicAPI]
public interface IDownloader
{
    /// <summary>
    /// Asynchronously starts a download.
    /// </summary>
    Task StartAsync(IDownloadActivity downloadActivity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously pauses a download.
    /// </summary>
    Task PauseAsync(IDownloadActivity downloadActivity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously cancels a download.
    /// </summary>
    Task CancelAsync(IDownloadActivity downloadActivity, CancellationToken cancellationToken = default);
}
