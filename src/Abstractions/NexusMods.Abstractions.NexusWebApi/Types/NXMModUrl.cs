namespace NexusMods.Abstractions.NexusWebApi.Types;

/// <summary>
/// specialized variant of NXMUrl for mod urls
/// </summary>
// ReSharper disable once InconsistentNaming
public class NXMModUrl : NXMUrl
{
    /// <summary>
    /// id of the mod page
    /// </summary>
    public ModId ModId { get; set; }

    /// <summary>
    /// id of the file (within that game domain)
    /// </summary>
    public FileId FileId { get; set; }

    /// <summary>
    /// game domain (name of the game within the Nexus Mods page)
    /// </summary>
    public string Game { get; set; }

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="uri">parsed uri</param>
    /// <exception cref="ArgumentException">if the uri is not valid</exception>
    public NXMModUrl(Uri uri)
    {
        UrlType = NXMUrlType.Mod;
        if (uri.Segments is not [_, _, _, "files/", _])
        {
            throw new ArgumentException($"invalid nxm url \"{uri}\"");
        }
        Game = uri.Host;
        try
        {
            ModId = ModId.From(ulong.Parse(uri.Segments[2].TrimEnd('/')));
            FileId = FileId.From(ulong.Parse(uri.Segments[4].TrimEnd('/')));
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
        return $"nxm://{Game}/mods/{ModId}/files/{FileId}?{QueryString}";
    }
}
