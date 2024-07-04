using JetBrains.Annotations;
using NexusMods.Abstractions.Activities;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Represents ephemeral data about ongoing downloads and their interactions.
/// </summary>
/// <seealso cref="PersistedDownloadState"/>
[PublicAPI]
public interface IDownloadActivity : IReactiveObject
{
    /// <summary>
    /// Gets the ID of the persisted download state.
    /// </summary>
    PersistedDownloadStateId PersistedStateId { get; }

    /// <summary>
    /// Gets the downloader that handles this activity.
    /// </summary>
    IDownloader Downloader { get; }

    /// <summary>
    /// Gets the status of the download.
    /// </summary>
    PersistedDownloadStatus Status { get; set; }

    /// <summary>
    /// Gets the total amount of bytes of the download.
    /// </summary>
    Size BytesTotal { get; set; }

    /// <summary>
    /// Gets the amount of bytes that have been downloaded.
    /// </summary>
    Size BytesDownloaded { get; set; }

    /// <summary>
    /// Gets the amount of bytes that still need to be downloaded.
    /// </summary>
    Size BytesRemaining { get; set; }

    /// <summary>
    /// Gets the current progress as a percentage.
    /// </summary>
    Percent Progress { get; set; }

    /// <summary>
    /// Gets the current download speed.
    /// </summary>
    Bandwidth Bandwidth { get; set; }
}
