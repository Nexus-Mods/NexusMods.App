using Microsoft.Extensions.Logging;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader;

/// <summary>
/// A simple implementation of <see cref="IHttpDownloader"/> used for diagnostic
/// purposes, or as a fallback.
/// </summary>
public class SimpleHttpDownloader : IHttpDownloader
{
    private readonly ILogger<SimpleHttpDownloader> _logger;
    private readonly HttpClient _client;
    private readonly IResource<IHttpDownloader, Size> _limiter;

    /// <summary/>
    /// <param name="logger">Logger for the download operations.</param>
    /// <param name="client">The client which will be used to issue download requests.</param>
    /// <param name="limiter">Limiter for the concurrent jobs we can run at once.</param>
    /// <remarks>This constructor is usually called from DI container.</remarks>
    public SimpleHttpDownloader(ILogger<SimpleHttpDownloader> logger, HttpClient client,
        IResource<IHttpDownloader, Size> limiter)
    {
        _logger = logger;
        _client = client;
        _limiter = limiter;
    }

    /// <inheritdoc />
    public async Task<Hash> DownloadAsync(IReadOnlyList<HttpRequestMessage> sources, AbsolutePath destination, HttpDownloaderState? state, Size? size, CancellationToken token)
    {
        state ??= new HttpDownloaderState();
        foreach (var source in sources)
        {
            using var job = await _limiter.BeginAsync($"Downloading {destination.FileName}", size ?? Size.One, token);

            // Note: If download fails, job will be reported as 'failed', and will not participate in throughput calculations.
            state.Job = job;

            var response = await _client.SendAsync(source, HttpCompletionOption.ResponseHeadersRead, token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download {Source} to {Destination}: {StatusCode}", source.RequestUri, destination, response.StatusCode);
                continue;
            }

            job.Size = size ?? Size.FromLong(response.Content.Headers.ContentLength ?? 1);

            await using var stream = await response.Content.ReadAsStreamAsync(token);
            await using var file = destination.Create();
            return await stream.HashingCopyAsync(file, token, job);
        }

        throw new Exception($"Could not download {destination.FileName}");
    }
}
