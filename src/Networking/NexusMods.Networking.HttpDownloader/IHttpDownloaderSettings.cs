using JetBrains.Annotations;
using NexusMods.Paths.Utilities;

namespace NexusMods.Networking.HttpDownloader;

/// <summary>
/// Settings for the HTTP downloader.
/// </summary>
[PublicAPI]
public interface IHttpDownloaderSettings
{
    /// <summary>
    /// Number of chunks to use while downloading. More chunks might mean larger server load.
    /// </summary>
    public int ChunkCount { get; }

    /// <summary>
    /// How many blocks can be queued to be written to disk. If this is too low, temporary disk slowdown could lead to
    /// worse download speed as the disk throttles the download threads. If this is high, consistently slow disk
    /// (NAS, external drives) or extremely fast internet would lead to high memory usage.
    /// </summary>
    public int WriteQueueLength { get; }

    /// <summary>
    /// Minimum age in milliseconds of a download before it may be canceled for being slow.
    /// </summary>
    public int MinCancelAge { get; }

    /// <summary>
    /// The relative speed compared to the fastest chunk below which a chunk may be canceled
    /// </summary>
    public double CancelSpeedFraction { get; }
}

/// <summary>
/// Default implementation of <see cref="IHttpDownloaderSettings"/> for reference.
/// </summary>
[PublicAPI]
public class HttpDownloaderSettings : IHttpDownloaderSettings
{
    /// <inheritdoc />
    public int ChunkCount { get; set; } = 4;

    /// <inheritdoc />
    public int WriteQueueLength { get; set; } = 16;

    /// <inheritdoc />
    public int MinCancelAge { get; set; } = 500;

    /// <inheritdoc />
    public double CancelSpeedFraction { get; set; } = 0.66;

    /// <summary>
    /// Expands any user provided paths; and ensures default settings in case of placeholders.
    /// </summary>
    public void Sanitize()
    {
        ChunkCount = ChunkCount <= 0 ? 4 : ChunkCount;
        WriteQueueLength = WriteQueueLength <= 0 ? 16 : WriteQueueLength;
        MinCancelAge = MinCancelAge <= 0 ? 500 : MinCancelAge;
        CancelSpeedFraction = CancelSpeedFraction <= 0 ? 0.66 : CancelSpeedFraction;
    }
}
