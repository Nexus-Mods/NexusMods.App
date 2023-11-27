using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Activities;
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
    private readonly IActivityFactory _activityFactory;

    /// <summary/>
    /// <param name="logger">Logger for the download operations.</param>
    /// <param name="client">The client which will be used to issue download requests.</param>
    /// <param name="activityFactory">Limiter for the concurrent jobs we can run at once.</param>
    /// <remarks>This constructor is usually called from DI container.</remarks>
    public SimpleHttpDownloader(ILogger<SimpleHttpDownloader> logger, HttpClient client,
        IActivityFactory activityFactory)
    {
        _logger = logger;
        _client = client;
        _activityFactory = activityFactory;
    }

    /// <inheritdoc />
    public async Task<Hash> DownloadAsync(IReadOnlyList<HttpRequestMessage> sources, AbsolutePath destination, HttpDownloaderState? state, Size? size, CancellationToken token)
    {
        state ??= new HttpDownloaderState();
        foreach (var source in sources)
        {
            using var job = _activityFactory.Create<Size>(IHttpDownloader.Group, "Downloading {FileName}", destination.FileName);

            // Note: If download fails, job will be reported as 'failed', and will not participate in throughput calculations.
            state.Activity = job;

            var response = await _client.SendAsync(source, HttpCompletionOption.ResponseHeadersRead, token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download {Source} to {Destination}: {StatusCode}", source.RequestUri, destination, response.StatusCode);
                continue;
            }

            job.SetMax(size ?? Size.FromLong(response.Content.Headers.ContentLength ?? 1));

            await using var stream = await response.Content.ReadAsStreamAsync(token);
            await using var file = destination.Create();
            return await stream.HashingCopyAsync(file, job, token);
        }

        throw new Exception($"Could not download {destination.FileName}");
    }
}
