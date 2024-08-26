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
    private static readonly HttpClient HttpClient = new();

    /// <inheritdoc />
    public async Task<Hash> DownloadAsync(
        IReadOnlyList<HttpRequestMessage> sources,
        AbsolutePath destination,
        HttpDownloaderState? state,
        Size? size,
        CancellationToken cancellationToken)
    {
        var url = sources[0].RequestUri!.ToString();

        await using var inStream = await HttpClient.GetStreamAsync(url, cancellationToken: cancellationToken);
        await using var outStream = destination.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

        await inStream.CopyToAsync(outStream, cancellationToken: cancellationToken);

        return await destination.XxHash64Async(token: cancellationToken);
    }
}
