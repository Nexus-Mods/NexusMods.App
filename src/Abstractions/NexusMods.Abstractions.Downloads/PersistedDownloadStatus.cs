using JetBrains.Annotations;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Status of a persisted download.
/// </summary>
[PublicAPI]
public enum PersistedDownloadStatus : byte
{
    /// <summary>
    /// Default status, the download is paused.
    /// </summary>
    Paused = 0,

    /// <summary>
    /// The download is progressing.
    /// </summary>
    Downloading = 1,

    /// <summary>
    /// The download is cancelled.
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// The download is completed.
    /// </summary>
    Completed = 3,
}
