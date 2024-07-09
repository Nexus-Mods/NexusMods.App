using JetBrains.Annotations;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Status of a persisted download.
/// </summary>
[PublicAPI]
public enum PersistedDownloadStatus : byte
{
    /// <summary>
    /// Default status, the download has been created but not started.
    /// </summary>
    Created = 0,

    /// <summary>
    /// The download is running.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The download is paused.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// The download is completed.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// The download was cancelled by the user.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// The download failed.
    /// </summary>
    Failed = 5,
}
