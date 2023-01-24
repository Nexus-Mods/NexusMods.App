namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Generic interface for creating <see cref="HttpRequestMessage"/>s for Nexus requests, will append
/// headers to the request as required to authenticate with the Nexus API
/// </summary>
public interface IHttpMessageFactory
{
    /// <summary>
    /// Creates a new <see cref="HttpRequestMessage"/> for the given <paramref name="method"/> and <paramref name="uri"/>
    /// </summary>
    /// <param name="method"></param>
    /// <param name="uri"></param>
    /// <returns></returns>
    public ValueTask<HttpRequestMessage> Create(HttpMethod method, Uri uri);
    
    public ValueTask<bool> IsAuthenticated();
}