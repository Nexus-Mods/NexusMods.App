using Downloader;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader;

/// <summary>
/// A simple implementation of <see cref="IHttpDownloader"/> used for diagnostic
/// purposes, or as a fallback.
/// </summary>
[Obsolete(message: "To be replaced with Jobs and an easier implementation using the Downloader package")]
public class SimpleHttpDownloader : IHttpDownloader
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public SimpleHttpDownloader(ILogger<SimpleHttpDownloader> logger) { }

    /// <inheritdoc />
    public async Task<Hash> DownloadAsync(
        IReadOnlyList<HttpRequestMessage> sources,
        AbsolutePath destination,
        HttpDownloaderState? state,
        Size? size,
        CancellationToken cancellationToken)
    {
        state ??= new HttpDownloaderState();
        var activity = (IActivitySource<Size>?)state.Activity;

        var downloadService = new DownloadService(new DownloadConfiguration
        {
            // TODO: find good values, probably put some in settings
            ChunkCount = 4,
            ParallelDownload = true,
            Timeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds,

            ReserveStorageSpaceBeforeStartingDownload = true,
        });

        downloadService.DownloadStarted += (_, args) =>
        {
            activity?.SetMax(Size.FromLong(args.TotalBytesToReceive));
        };

        var lastUpdate = DateTime.MinValue;
        downloadService.DownloadProgressChanged += (_, args) =>
        {
            // TODO: remove this, this is a hack to keep our UI from exploding
            var now = DateTime.Now;
            if (now - lastUpdate < TimeSpan.FromMilliseconds(700)) return;
            activity?.SetProgress(Size.FromLong(args.ReceivedBytesSize));
        };

        // TODO: figure out what do with multiple sources
        var urls = sources.Select(source => source.RequestUri!.ToString()).Take(1).ToArray();

        await downloadService.DownloadFileTaskAsync(
            urls: urls,
            fileName: destination.ToNativeSeparators(OSInformation.Shared),
            cancellationToken: cancellationToken
        );

        return await destination.XxHash64Async(token: cancellationToken);
    }
}
