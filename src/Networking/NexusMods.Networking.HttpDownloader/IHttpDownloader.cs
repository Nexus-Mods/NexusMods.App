using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader;

/// <summary>
/// Represents a HTTP downloader
/// </summary>
public interface IHttpDownloader
{
    /// <summary>
    /// Download the file specified by the given uris and save it to the given destination. Returns the hash of the downloaded file.
    /// If multiple source are provided, they are assumed to be mirrors, and the downloader is free to load balance between them.
    /// They are assumed to be in order of preference, with the first being the most preferred.
    /// </summary>
    /// <param name="sources"></param>
    /// <param name="destination"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Hash> Download(IReadOnlyList<HttpRequestMessage> sources, AbsolutePath destination, Size? size = null, CancellationToken token = default);
}