using Downloader;
using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;
using NexusMods.App.BuildInfo;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader;

/// <summary>
/// Settings for <see cref="HttpDownloadJobWorker"/>.
/// </summary>
[PublicAPI]
public record HttpDownloaderSettings : ISettings
{
    public int ChunkCount { get; set; } = 4;

    public Size BufferBlockSize { get; set; } = Size.KB * 8;

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
            BufferBlockSize = (int)BufferBlockSize.Value,
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
