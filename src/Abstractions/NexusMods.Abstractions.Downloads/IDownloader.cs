using JetBrains.Annotations;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Represents a downloader.
/// </summary>
[PublicAPI]
public interface IDownloader
{
    /// <summary>
    /// Starts a download.
    /// </summary>
    /// <remarks>
    /// This method is non-blocking and returns instantly after the download has been enqueued.
    /// </remarks>
    void Start(IDownloadActivity downloadActivity);

    /// <summary>
    /// Pauses a download.
    /// </summary>
    void Pause(IDownloadActivity downloadActivity);

    /// <summary>
    /// Cancels a download.
    /// </summary>
    void Cancel(IDownloadActivity downloadActivity);
}
