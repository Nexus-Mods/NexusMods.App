using Downloader;
using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;
using NexusMods.App.BuildInfo;

namespace NexusMods.Networking.HttpDownloader;

/// <summary>
/// Settings for <see cref="HttpDownloadJobWorker"/>.
/// </summary>
[PublicAPI]
public record HttpDownloaderSettings : ISettings
{
    public int ChunkCount { get; set; } = 4;

    public bool ParallelDownload { get; set; } = true;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <inheritdoc/>
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder;
    }

    internal DownloadConfiguration ToConfiguration()
    {
        return new DownloadConfiguration
        {
            ChunkCount = ChunkCount,
            BufferBlockSize = 1024 * 8,
            ParallelDownload = ParallelDownload,
            Timeout = (int)Timeout.TotalMilliseconds,

            ReserveStorageSpaceBeforeStartingDownload = true,
            CheckDiskSizeBeforeDownload = true,

            RequestConfiguration = new RequestConfiguration
            {
                AllowAutoRedirect = true,
                MaximumAutomaticRedirections = 3,

                UserAgent = ApplicationConstants.UserAgent,
            },
        };
    }
}
