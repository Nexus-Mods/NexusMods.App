using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Abstractions.HttpDownloader;

/// <summary>
/// Settings for the HTTP downloader.
/// </summary>
[PublicAPI]
public class HttpDownloaderSettings : ISettings
{
    /// <summary>
    /// How many blocks can be queued to be written to disk. If this is too low, temporary disk slowdown could lead to
    /// worse download speed as the disk throttles the download threads. If this is high, consistently slow disk
    /// (NAS, external drives) or extremely fast internet would lead to high memory usage.
    /// </summary>
    public int WriteQueueLength { get; init; } = 16;

    /// <summary>
    /// Minimum age in milliseconds of a download before it may be canceled for being slow.
    /// </summary>
    public int MinCancelAge { get; init; } = 500;

    /// <summary>
    /// The relative speed compared to the fastest chunk below which a chunk may be canceled
    /// </summary>
    public double CancelSpeedFraction { get; init; } = 0.66;

    /// <inheritdoc/>
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // TODO: show this be exposed in the UI?
        return settingsBuilder;
    }
}
