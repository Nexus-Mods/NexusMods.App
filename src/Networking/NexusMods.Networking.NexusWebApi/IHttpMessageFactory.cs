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
    public ValueTask<HttpRequestMessage> Create(HttpMethod method, Uri uri);
    
    /// <summary>
    /// Returns true if the user is authenticated [has a saved or set API key]; else false.
    /// </summary>
    public ValueTask<bool> IsAuthenticated();
}