using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.Extensions.Http.Resilience;
using NexusMods.Abstractions.Resources;
using Polly;

namespace NexusMods.DataModel;

[PublicAPI]
public class HttpLoader : IResourceLoader<Uri, byte[]>
{
    private readonly HttpClient _httpClient;
    private const string SupportedScheme = "https";

    /// <summary>
    /// Constructor.
    /// </summary>
    public HttpLoader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public static HttpLoader CreateDefault()
    {
        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions())
            .AddTimeout(new HttpTimeoutStrategyOptions())
            .Build();

#pragma warning disable EXTEXP0001
        HttpMessageHandler handler = new ResilienceHandler(pipeline)
#pragma warning restore EXTEXP0001
        {
            InnerHandler = new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(10),
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
                KeepAlivePingDelay = TimeSpan.FromSeconds(5),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(20),
            },
        };

        var client = new HttpClient(handler);
        return new HttpLoader(client);
    }

    /// <inheritdoc/>
    public async ValueTask<Resource<byte[]>> LoadResourceAsync(Uri resourceIdentifier, CancellationToken cancellationToken)
    {
        Debug.Assert(resourceIdentifier.Scheme.Equals(SupportedScheme, StringComparison.OrdinalIgnoreCase));

        using var responseMessage = await _httpClient.GetAsync(resourceIdentifier, HttpCompletionOption.ResponseContentRead, cancellationToken);
        responseMessage.EnsureSuccessStatusCode();

        using var content = responseMessage.Content;
        var bytes = await content.ReadAsByteArrayAsync(cancellationToken: cancellationToken);

        return new Resource<byte[]>
        {
            Data = bytes,
            ExpiresAt = GetExpiresAt(responseMessage),
        };
    }

    private static DateTime GetExpiresAt(HttpResponseMessage responseMessage)
    {
        var cacheControl = responseMessage.Headers.CacheControl;
        if (cacheControl is null) return DateTime.MaxValue;

        var maxAge = cacheControl.MaxAge;
        if (!maxAge.HasValue) return DateTime.MaxValue;

        var age = responseMessage.Headers.Age;

        var diff = maxAge.Value;
        if (age.HasValue) diff -= age.Value;

        return DateTime.UtcNow + diff;
    }
}
