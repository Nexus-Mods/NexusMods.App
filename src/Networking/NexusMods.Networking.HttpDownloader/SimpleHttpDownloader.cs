using Microsoft.Extensions.Logging;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader;

public class SimpleHttpDownloader : IHttpDownloader
{
    private readonly ILogger<SimpleHttpDownloader> _logger;
    private readonly HttpClient _client;
    private readonly IResource<IHttpDownloader, Size> _limiter;

    public SimpleHttpDownloader(ILogger<SimpleHttpDownloader> logger, HttpClient client,
        IResource<IHttpDownloader, Size> limiter)
    {
        _logger = logger;
        _client = client;
        _limiter = limiter;
    }

    public async Task<Hash> Download(IReadOnlyList<HttpRequestMessage> sources, AbsolutePath destination, Size? size, CancellationToken token)
    {
        foreach (var source in sources)
        {
            //_logger.LogDebug("Downloading {Source} to {Destination}", source.RequestUri, destination);

            using var job = await _limiter.BeginAsync($"Downloading {destination.FileName}", size ?? Size.One, token);
            var response = await _client.SendAsync(source, HttpCompletionOption.ResponseHeadersRead, token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download {Source} to {Destination}: {StatusCode}", source.RequestUri, destination, response.StatusCode);
                continue;
            }

            job.Size = size ?? Size.From(response.Content.Headers.ContentLength ?? 1);

            await using var stream = await response.Content.ReadAsStreamAsync(token);
            await using var file = destination.Create();
            return await stream.HashingCopy(file, token, job);
        }

        throw new Exception($"Could not download {destination.FileName}");
    }
}
