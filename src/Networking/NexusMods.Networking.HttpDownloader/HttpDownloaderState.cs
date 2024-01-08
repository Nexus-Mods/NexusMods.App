using NexusMods.Abstractions.Activities;
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

    /// <summary>
    /// The read-only job associated with the current HTTP Downloader Progress.
    /// </summary>
    public IReadOnlyActivity<Size>? ActivityStatus
    {
        get
        {
            if (Activity is null) return null;

            if (Activity is IReadOnlyActivity<Size> casted) return casted;
            return null;
        }
    }
}
