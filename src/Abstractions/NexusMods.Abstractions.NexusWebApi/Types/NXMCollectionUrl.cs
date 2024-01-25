namespace NexusMods.Abstractions.NexusWebApi.Types;

/// <summary>
/// specialized variant of NXMUrl for collecton links
/// </summary>
// ReSharper disable once InconsistentNaming
public class NXMCollectionUrl : NXMUrl
{
    /// <summary>
    /// string uniquely identifying a collection
    /// </summary>
    public CollectionSlug Slug { get; set; }

    /// <summary>
    /// the revision number (this is not the global revision id, the revision number is  like a version number within a collection)
    /// </summary>
    public RevisionNumber Revision { get; set; }

    /// <summary>
    /// name of the game within the Nexus Mods site
    /// </summary>
    public string Game { get; set; }

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="uri">parsed uri</param>
    /// <exception cref="ArgumentException">if the uri is not valid</exception>
    public NXMCollectionUrl(Uri uri)
    {
        UrlType = NXMUrlType.Collection;
        if (uri.Segments is not [_, _, _, "revisions/", _])
        {
            throw new ArgumentException($"invalid nxm url \"{uri}\"");
        }
        Game = uri.Host;
        try
        {
            Slug = CollectionSlug.From(uri.Segments[2].TrimEnd('/'));
            Revision = RevisionNumber.From(ulong.Parse(uri.Segments[4].TrimEnd('/')));
        }
        catch (FormatException)
        {
            throw new ArgumentException($"invalid nxm url \"{uri}\"");
        }
    }

    /// <summary>
    /// serialize the url
    /// </summary>
    public override string ToString()
    {
        return $"nxm://{Game}/collections/{Slug}/revisions/{Revision}?{QueryString}";
    }
}
