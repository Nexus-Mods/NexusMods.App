namespace NexusMods.Abstractions.NexusWebApi;

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

    /// <summary>
    /// allows the message factory to hande an error.
    /// Implementations have to take care that this doesn't lead into an endless loop!
    /// </summary>
    /// <param name="original">the original request that led to this error</param>
    /// <param name="ex">the exception to handle (or not)</param>
    /// <param name="cancel">cancellation token</param>
    /// <returns>a new/updated request to be sent or null if the exception should be thrown</returns>
    public ValueTask<HttpRequestMessage?> HandleError(HttpRequestMessage original, HttpRequestException ex, CancellationToken cancel);
}
