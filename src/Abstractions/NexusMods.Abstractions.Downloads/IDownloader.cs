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
    Task StartAsync(IDownloadActivity downloadActivity);

    /// <summary>
    /// Asynchronously pauses a download.
    /// </summary>
    Task PauseAsync(IDownloadActivity downloadActivity);

    /// <summary>
    /// Asynchronously cancels a download.
    /// </summary>
    Task CancelAsync(IDownloadActivity downloadActivity);
}
