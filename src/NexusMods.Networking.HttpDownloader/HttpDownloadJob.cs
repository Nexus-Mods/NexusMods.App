using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.HttpDownloads;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using Polly;
using Polly.Retry;

namespace NexusMods.Networking.HttpDownloader;

/// <summary>
/// Download job.
/// </summary>
[PublicAPI]
public record HttpDownloadJob : IJobDefinitionWithStart<HttpDownloadJob, AbsolutePath>, IHttpDownloadJob
{
    private static readonly ResiliencePipeline<AbsolutePath> ResiliencePipeline = BuildResiliencePipeline();

    /// <summary>
    /// Client.
    /// </summary>
    public required HttpClient Client { get; init; }

    /// <summary>
    /// Logger.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    /// The uri of the download page.
    /// </summary>
    public required Uri Uri { get; init; }
    
    /// <summary>
    /// The uri of the download page.
    /// </summary>
    public required Uri DownloadPageUri { get; init; }
    
    /// <summary>
    /// The destination of the download.
    /// </summary>
    public required AbsolutePath Destination { get; init; }
    
    /// <summary>
    /// Only exists for extension by derived classes.
    /// </summary>
    public virtual ValueTask AddMetadata(ITransaction transaction, LibraryFile.New libraryFile)
    {
        return ValueTask.CompletedTask;
    }

    private Optional<Size> ContentLength { get; set; }
    private Optional<EntityTagHeaderValue> ETag { get; set; }
    private Optional<bool> AcceptRanges { get; set; }
    
    private Size TotalBytesDownloaded { get; set; }
    
    /// <summary>
    /// Constructor for the job
    /// </summary>
    public static IJobTask<HttpDownloadJob, AbsolutePath> Create(
        IServiceProvider provider,
        Uri uri,
        Uri downloadPage,
        AbsolutePath destination)
    {
        var monitor = provider.GetRequiredService<IJobMonitor>();
        var job = new HttpDownloadJob
        {
            Uri = uri,
            DownloadPageUri = downloadPage,
            Destination = destination,
            Logger = provider.GetRequiredService<ILogger<HttpDownloadJob>>(),
            Client = provider.GetRequiredService<HttpClient>(),
        };

        return monitor.Begin<HttpDownloadJob, AbsolutePath>(job);
    }

    /// <inheritdoc/>
    public async ValueTask<AbsolutePath> StartAsync(IJobContext<HttpDownloadJob> context)
    {
        var result = await ResiliencePipeline.ExecuteAsync(
            callback: static (tuple, _) =>
            {
                var (self, context) = tuple;
                return self.StartAsyncImpl(context);
            },
            state: (this, context),
            cancellationToken: context.CancellationToken
        );

        return result;
    }

    private async ValueTask<AbsolutePath> StartAsyncImpl(IJobContext<HttpDownloadJob> context)
    {
        await context.YieldAsync();
        await FetchMetadata(context);

        await context.YieldAsync();
        await using var fileStream = Destination.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

        await using var outputStream = new StreamProgressWrapper<IJobContext<HttpDownloadJob>>(
            fileStream,
            context,
            (state, tuple) =>
        {
            var (bytesWritten, speed) = tuple;

            TotalBytesDownloaded = bytesWritten;
            state.SetPercent(bytesWritten, ContentLength.ValueOr(static () => Size.One));
            state.SetRateOfProgress(speed);
        });

        if (ContentLength.HasValue)
        {
            var contentLength = (long)ContentLength.Value.Value;
            if (outputStream.Length != contentLength) outputStream.SetLength(contentLength);
        }

        outputStream.Position = (long)TotalBytesDownloaded.Value;

        await context.YieldAsync();
        using var request = PrepareRequest(out var isRangeRequest);
        using var response = await Client.SendAsync(
            request: request,
            completionOption: HttpCompletionOption.ResponseHeadersRead,
            cancellationToken: context.CancellationToken
        );

        response.EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.OK)
        {
            // NOTE(erri120): We might've not gotten the content length in the initial HEAD request.
            if (!ContentLength.HasValue)
            {
                // For full requests only, the response should contain the content length at this point.
                // For range requests, the content length equals the length of the range, so we can't use that here.
                var newContentLength = response.Content.Headers.ContentLength;
                if (newContentLength is not null)
                {
                    ContentLength = Size.FromLong(newContentLength.Value);
                    outputStream.SetLength(newContentLength.Value);
                }
            }
        } else if (response.StatusCode == HttpStatusCode.PartialContent)
        {
            if (!ContentLength.HasValue)
            {
                // Responses to range requests should have this header
                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Range
                var contentRange = response.Content.Headers.ContentRange;
                var length = contentRange?.Length;
                if (length is not null)
                {
                    ContentLength = Size.FromLong(length.Value);
                    outputStream.SetLength(length.Value);
                }
            }
        }

        if (isRangeRequest && response.StatusCode == HttpStatusCode.OK)
        {
            // NOTE(erri120): We asked the server whether it supports range requests, the server responded with yes,
            // then we do a range request, and suddenly the server changed its mind and says no...
            Logger.LogWarning("Server `{ServerName}` responded with 200 to a valid range request for download from `{PageUri}`. The download will be reset", response.Headers.Server.ToString(), DownloadPageUri);

            // NOTE(erri120): The only thing we can do here is to reset everything and start from scratch.
            TotalBytesDownloaded = Size.Zero;
            outputStream.Position = 0;
            AcceptRanges = false;
        }

        try
        {
            await response.Content.CopyToAsync(outputStream, context.CancellationToken);
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Exception while downloading from `{PageUri}`, downloaded `{DownloadedBytes}` from `{TotalBytes}` bytes", DownloadPageUri, outputStream.Position, outputStream.Length);
            throw;
        }
        finally
        {
            TotalBytesDownloaded = Size.FromLong(outputStream.Position);
        }

        return Destination;
    }
    
    private HttpRequestMessage PrepareRequest(out bool isRangeRequest)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Uri);

        // NOTE(erri120): use a normal GET request for the entire file
        if (!AcceptRanges.Value || TotalBytesDownloaded == Size.Zero)
        {
            // NOTE(erri120): Using If-Match to ensure that what we're downloading didn't suddenly change
            if (ETag.HasValue)
            {
                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-Match
                request.Headers.IfMatch.Add(ETag.Value);
            }

            isRangeRequest = false;
        }
        else
        {
            // NOTE(erri120): Using If-Range for range requests instead of If-Match
            if (ETag.HasValue)
            {
                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-Range
                request.Headers.IfRange = new RangeConditionHeaderValue(ETag.Value);
            }

            // A server MAY send a Content-Length header field in a response to a HEAD request
            // https://httpwg.org/specs/rfc9110.html#rfc.section.8.6

            // NOTE(erri120): As such, we might not know the content length, but since we download
            // in serial, we can omit the end position to request all remaining bytes
            var range = ContentLength.HasValue
                ? new RangeHeaderValue(
                    from: (long)TotalBytesDownloaded.Value,
                    to: (long)ContentLength.Value.Value
                )
                : new RangeHeaderValue(
                    from: (long)TotalBytesDownloaded.Value,
                    to: null
                );

            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Range
            request.Headers.Range = range;
            isRangeRequest = true;
        }

        return request;
    }

    private async ValueTask FetchMetadata(IJobContext context)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, Uri);
        using var response = await Client.SendAsync(
            request: request,
            completionOption: HttpCompletionOption.ResponseHeadersRead,
            cancellationToken: context.CancellationToken
        ).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        // https://datatracker.ietf.org/doc/html/rfc7233#section-5.1.2
        // "bytes" and "none" are the only range units we care about,
        // "none" meaning the server doesn't support ranges
        AcceptRanges = response.Headers.AcceptRanges.Contains("bytes");
        ETag = response.Headers.ETag;

        var contentLength = response.Content.Headers.ContentLength;
        ContentLength = contentLength is not null ? Size.FromLong(contentLength.Value) : Optional<Size>.None;
    }

    private static ResiliencePipeline<AbsolutePath> BuildResiliencePipeline()
    {
        ImmutableArray<Type> networkExceptions =
        [
            typeof(HttpIOException),
            typeof(HttpRequestException),
            typeof(SocketException),
        ];

        var pipeline = new ResiliencePipelineBuilder<AbsolutePath>()
            .AddRetry(new RetryStrategyOptions<AbsolutePath>
            {
                ShouldHandle = args => ValueTask.FromResult(args.Outcome.Exception is not null && networkExceptions.Contains(args.Outcome.Exception.GetType())),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(3),
            })
            .Build();

        return pipeline;
    }
}
