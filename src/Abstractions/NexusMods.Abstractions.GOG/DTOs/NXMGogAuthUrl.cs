using NexusMods.Abstractions.NexusWebApi.Types;

namespace NexusMods.Abstractions.GOG.DTOs;

/// <summary>
/// The GOG authentication callback URL.
/// </summary>
public class NXMGogAuthUrl : NXMUrl
{
    /// <summary>
    /// a code. This gets used in another request to get at the actual authorization code
    /// </summary>
    public string? Code => Query.Get("code");
    
    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="uri">parsed url</param>
    public NXMGogAuthUrl(Uri uri)
    {
        UrlType = NXMUrlType.OAuth;
        if (!Query.AllKeys.Contains("code"))
            throw new ArgumentException($"invalid nxm url \"{uri}\"");
            
    }

    /// <summary>
    /// serialize the url
    /// </summary>
    public override string ToString() => $"nxm://gog-auth/?code={QueryString}";    
}
