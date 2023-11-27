using NexusMods.Abstractions.Activities;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader;

/// <summary>
/// Represents a HTTP downloader implementation.
/// </summary>
public interface IHttpDownloader
{
    /// <summary>
    /// The activity group for HTTP downloader activities.
    /// </summary>
    public static readonly ActivityGroup Group = ActivityGroup.From("HttpDownloader");

    /// <summary>
    /// Download the file specified by the given requests and save it to the
    /// given destination. Returns the hash of the downloaded file.
    /// If multiple source are provided, they are assumed to be mirrors,
    /// and the downloader is free to load balance between them.
    ///
    /// They are assumed to be in order of preference, with the first being
    /// the most preferred.
    /// </summary>
    /// <param name="sources">Locations to download from.</param>
    /// <param name="destination">Where the file will be saved to.</param>
    /// <param name="state">Variable that receives the state of the downloader.</param>
    /// <param name="size">
    ///     Size of the file being saved. Used for tracking progress, might not always be known.
    /// </param>
    /// <param name="token">Allows you to cancel the saving operation.</param>
    /// <returns></returns>
    public Task<Hash> DownloadAsync(IReadOnlyList<HttpRequestMessage> sources, AbsolutePath destination, HttpDownloaderState? state = null, Size? size = null, CancellationToken token = default);

    /// <summary>
    /// Download the file specified by the given uris and save it to the
    /// given destination. Returns the hash of the downloaded file.
    /// If multiple source are provided, they are assumed to be mirrors,
    /// and the downloader is free to load balance between them.
    ///
    /// They are assumed to be in order of preference, with the first being
    /// the most preferred.
    /// </summary>
    /// <param name="sources">Locations to download from.</param>
    /// <param name="destination">Where the file will be saved to.</param>
    /// <param name="size">
    ///     Size of the file being saved. Used for tracking progress, might not always be known.
    /// </param>
    /// <param name="state">This parameter allows you to receive information about the current state of the downloader.</param>
    /// <param name="token">Allows you to cancel the saving operation.</param>
    /// <returns></returns>
    public Task<Hash> DownloadAsync(IEnumerable<Uri> sources, AbsolutePath destination, Size? size = null, HttpDownloaderState? state = null, CancellationToken token = default) =>
        DownloadAsync(sources.Select(u => new HttpRequestMessage(HttpMethod.Get, u)).ToArray(), destination, state, size, token);
}
