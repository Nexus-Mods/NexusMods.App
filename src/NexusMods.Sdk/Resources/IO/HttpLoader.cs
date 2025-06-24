using System.Diagnostics;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Resources;

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
