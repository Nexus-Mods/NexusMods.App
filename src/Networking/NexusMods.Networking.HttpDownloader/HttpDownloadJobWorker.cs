using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using DynamicData.Kernel;
using Microsoft.Extensions.Http.Resilience;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Settings;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using Polly;

namespace NexusMods.Networking.HttpDownloader;

public class HttpDownloadJobWorker : APersistedJobWorker<HttpDownloadJob>
{
#pragma warning disable EXTEXP0001
    private static readonly HttpClient Client = BuildClient();
#pragma warning restore EXTEXP0001

    private readonly IConnection _connection;
    private readonly IJobMonitor _jobMonitor;
    private readonly ISettingsManager _settingsManager;

    /// <summary>
    /// Constructor.
    /// </summary>
    public HttpDownloadJobWorker(IConnection connection, IJobMonitor jobMonitor, ISettingsManager settingsManager)
    {
        _connection = connection;
        _jobMonitor = jobMonitor;
        _settingsManager = settingsManager;
        ProgressRateFormatter = new BytesPerSecondFormatter();
    }

    /// <inheritdoc/>
    public override Guid Id { get; } = Guid.Parse("da685bec-d369-4a7c-870c-26edb879f832");

    /// <inheritdoc/>
    protected override async Task<JobResult> ExecuteAsync(HttpDownloadJob job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!job.AcceptRanges.HasValue)
        {
            await FetchMetadata(job, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var fileStream = job.Destination.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

        var progress = GetDeterminateProgress(job);

        await using var outputStream = new StreamProgressWrapper<(HttpDownloadJobWorker, HttpDownloadJob, DeterminateProgress)>(fileStream, (this, job, progress), static (state, tuple) =>
        {
            var (worker, job, determinateProgress) = state;
            var (bytesWritten, speed) = tuple;

            job.TotalBytesDownloaded = bytesWritten;
            var percent = Percent.Create(job.TotalBytesDownloaded.Value, job.ContentLength.Value.Value);

            determinateProgress.SetPercent(percent);
            determinateProgress.SetProgressRate(new ProgressRate(speed, worker.ProgressRateFormatter.Value));
        });

        if (job.ContentLength.HasValue)
        {
            var contentLength = (long)job.ContentLength.Value.Value;
            if (outputStream.Length != contentLength) outputStream.SetLength(contentLength);
        }

        outputStream.Position = (long)job.TotalBytesDownloaded.Value;

        cancellationToken.ThrowIfCancellationRequested();
        using var request = PrepareRequest(job, out var isRangeRequest);
        using var response = await Client.SendAsync(
            request: request,
            completionOption: HttpCompletionOption.ResponseHeadersRead,
            cancellationToken: cancellationToken
        );

        response.EnsureSuccessStatusCode();

        // NOTE(erri120): We might've not gotten the content length in the initial HEAD request.
        if (!isRangeRequest && !job.ContentLength.HasValue)
        {
            // For full requests only, the response should contain the content length at this point.
            // For range requests, the content length equals the length of the range, so we can't use that here.
            var newContentLength = response.Content.Headers.ContentLength;
            if (newContentLength is not null)
            {
                job.ContentLength = Size.FromLong(newContentLength.Value);
                outputStream.SetLength(newContentLength.Value);
            }
        } else if (isRangeRequest && !job.ContentLength.HasValue)
        {
            // Responses to range requests should have this header
            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Range
            var contentRange = response.Content.Headers.ContentRange;
            var length = contentRange?.Length;
            if (length is not null)
            {
                job.ContentLength = Size.FromLong(length.Value);
                outputStream.SetLength(length.Value);
            }
        }

        if (isRangeRequest && response.StatusCode == HttpStatusCode.OK)
        {
            // TODO: The server accepts ranges but didn't return partial content
            // NOTE(erri120): we need to reset and download the entire thing again...
            // This scenario should be rare, and would indicate some serious server bullshit
            throw new NotSupportedException();
        }

        try
        {
            await response.Content.CopyToAsync(outputStream, cancellationToken);
        }
        finally
        {
            job.TotalBytesDownloaded = Size.FromLong(outputStream.Position);
        }

        return JobResult.CreateCompleted(job.Destination);
    }

    private static HttpRequestMessage PrepareRequest(HttpDownloadJob job, out bool isRangeRequest)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, job.Uri);

        // NOTE(erri120): Our first request is a normal GET request that downloads the entire file for.
        // Follow-up requests are range requests, if the server allows it. A range response uses 206 Partial Content

        if (job.IsFirstRequest || !job.AcceptRanges.Value)
        {
            // NOTE(erri120): Using If-Match to ensure that what we're downloading didn't suddenly change
            if (job.ETag.HasValue)
            {
                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-Match
                request.Headers.IfMatch.Add(job.ETag.Value);
            }

            isRangeRequest = false;
        }
        else
        {
            // NOTE(erri120): Using If-Range for range requests instead of If-Match
            if (job.ETag.HasValue)
            {
                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-Range
                request.Headers.IfRange = new RangeConditionHeaderValue(job.ETag.Value);
            }

            // A server MAY send a Content-Length header field in a response to a HEAD request
            // https://httpwg.org/specs/rfc9110.html#rfc.section.8.6

            // NOTE(erri120): As such, we might not know the content length, but since we download
            // in serial, we can omit the end position to request all remaining bytes
            var range = job.ContentLength.HasValue
                ? new RangeHeaderValue(
                    from: (long)job.TotalBytesDownloaded.Value,
                    to: (long)job.ContentLength.Value.Value
                )
                : new RangeHeaderValue(
                    from: (long)job.TotalBytesDownloaded.Value,
                    to: null
                );

            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Range
            request.Headers.Range = range;
            isRangeRequest = true;
        }

        return request;
    }

    private static async ValueTask FetchMetadata(HttpDownloadJob job, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, job.Uri);
        using var response = await Client.SendAsync(
            request: request,
            completionOption: HttpCompletionOption.ResponseHeadersRead,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        // https://datatracker.ietf.org/doc/html/rfc7233#section-5.1.2
        // "bytes" and "none" are the only range units we care about,
        // "none" meaning the server doesn't support ranges
        job.AcceptRanges = response.Headers.AcceptRanges.Contains("bytes");
        job.ETag = response.Headers.ETag;

        var contentLength = response.Content.Headers.ContentLength;
        job.ContentLength = contentLength is not null ? Size.FromLong(contentLength.Value) : Optional<Size>.None;
    }

    /// <inheritdoc/>
    public override IJob LoadJob(PersistedJobState.ReadOnly state)
    {
        if (!state.TryGetAsHttpDownloadJobPersistedState(out var httpState))
            throw new NotSupportedException();

        return new HttpDownloadJob(
            _connection,
            httpState,
            worker: this,
            monitor: _jobMonitor
        );
    }

    [Experimental("EXTEXP0001")]
    private static HttpClient BuildClient()
    {
        // TODO: get values from settings, probably make this a singleton

        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions())
            .Build();

        HttpMessageHandler handler = new ResilienceHandler(pipeline)
        {
            InnerHandler = new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(10),
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
                KeepAlivePingDelay = TimeSpan.FromSeconds(5),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(20),
            },
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5),
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        };

        return client;
    }
}
