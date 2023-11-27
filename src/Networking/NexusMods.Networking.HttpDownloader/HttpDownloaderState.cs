using NexusMods.DataModel.Activities;
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
    public IActivitySource? Activity { get; set; }
}
