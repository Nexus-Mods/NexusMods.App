using NexusMods.DataModel.RateLimiting;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader;

/// <summary>
/// Utility for reporting the live state of the HTTP downloader.
/// </summary>
public class HttpDownloaderState
{
    /// <summary>
    /// The job associated with the current HTTP Downloader Progress.
    /// </summary>
    public IJob<IHttpDownloader, Size> Job { get; set; } = default!;
}
