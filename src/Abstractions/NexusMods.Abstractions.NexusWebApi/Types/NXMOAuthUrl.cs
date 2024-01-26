namespace NexusMods.Abstractions.NexusWebApi.Types;

/// <summary>
/// specialized variant of NXMUrl for oauth callback links
/// </summary>
// ReSharper disable once IdentifierTypo
// ReSharper disable once InconsistentNaming
public class NXMOAuthUrl : NXMUrl
{
    /// <summary>
    /// a code. This gets used in another request to get at the actual authorization code
    /// </summary>
    public string? Code => Query.Get("code");

    /// <summary>
    /// this is a random id we generated when initially creating the request, it allows us to
    /// match the respons to the initial request we made
    /// </summary>
    public string? State => Query.Get("state");

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="uri">parsed url</param>
    public NXMOAuthUrl(Uri uri)
    {
        UrlType = NXMUrlType.OAuth;
        if (uri.Segments is not [_, "callback"])
            throw new ArgumentException($"invalid nxm url \"{uri}\"");
    }

    /// <summary>
    /// serialize the url
    /// </summary>
    public override string ToString() => $"nxm://oauth/callback?{QueryString}";
}
