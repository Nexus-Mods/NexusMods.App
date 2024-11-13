using System.Diagnostics;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Resources.IO;

[PublicAPI]
public sealed class HttpLoader : IResourceLoader<Uri, byte[]>
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

    private static DateTimeOffset GetExpiresAt(HttpResponseMessage responseMessage)
    {
        var cacheControl = responseMessage.Headers.CacheControl;

        var maxAge = cacheControl?.MaxAge;
        if (!maxAge.HasValue) return DateTimeOffset.MaxValue;

        var age = responseMessage.Headers.Age;

        var diff = maxAge.Value;
        if (age.HasValue) diff -= age.Value;

        return TimeProvider.System.GetUtcNow() + diff;
    }
}
