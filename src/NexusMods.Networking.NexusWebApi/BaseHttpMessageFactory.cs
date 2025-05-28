using System.Net.Http.Headers;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.App.BuildInfo;

namespace NexusMods.Networking.NexusWebApi;

/// <inheritdoc/>
public class BaseHttpMessageFactory : IHttpMessageFactory
{
    // https://help.nexusmods.com/article/114-api-acceptable-use-policy
    internal const string HeaderApplicationVersion = "Application-Version";
    internal const string HeaderApplicationName = "Application-Name";

    /// <inheritdoc/>
    public virtual ValueTask<HttpRequestMessage> Create(HttpMethod method, Uri uri)
    {
        var result = new HttpRequestMessage(method, uri);
        result.Headers.Add(HeaderApplicationName, ApplicationConstants.UserAgentApplicationName);
        result.Headers.Add(HeaderApplicationVersion, ApplicationConstants.UserAgentApplicationVersion);

        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public virtual AuthenticationHeaderValue? GetAuthenticationHeaderValue() => null;

    /// <inheritdoc/>
    public virtual ValueTask<bool> IsAuthenticated() => ValueTask.FromResult(false);
}
