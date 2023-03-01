using NexusMods.DataModel.Games;
using System.Collections.Specialized;

namespace NexusMods.Networking.NexusWebApi.Types;

/// <summary>
/// type of url 
/// </summary>
public enum NXMUrlType : byte
{
    /// <summary>
    /// url referencing a mod file
    /// </summary>
    Mod = 0,
    /// <summary>
    /// url referencing a collection revision
    /// </summary>
    Collection,
    /// <summary>
    /// callback for oauth login
    /// </summary>
    OAuth
}

/// <summary>
/// parsed url of nxm:// protocol, used in multiple ways for the site communicating with clients
/// </summary>
public class NXMUrl
{
    /// <summary>
    /// url type
    /// </summary>
    public NXMUrlType UrlType { get; init; }

    /// <summary>
    /// if applicable, the time the url becomes invalid
    /// </summary>
    public DateTime? ExpireTime
    {
        get
        {
            string? expires = _query?.Get("expires");
            return expires != null ? DateTime.UnixEpoch.AddSeconds(ulong.Parse(expires)) : null;
        }
    }

    /// <summary>
    /// id of the user making the request, relevant for links tied to a user account
    /// (e.g. for free users downloading files)
    /// </summary>
    public UserId? User
    {
        get
        {
            string? user = _query?.Get("user_id");
            return user != null ? UserId.From(ulong.Parse(user)) : null;
        }
    }

    /// <summary>
    /// key for links tied to a user account
    /// (e.g. for free users downloading files)
    /// </summary>
    public NXMKey? Key
    {
        get
        {
            string? key = _query?.Get("key");
            return key != null ? NXMKey.From(key) : null;
        }
    }

    /// <summary>
    /// parsed query arguments in the url (everything after the ? in the url)
    /// </summary>
    protected NameValueCollection _query = new NameValueCollection();

    /// <summary>
    /// parse a nxm:// url
    /// </summary>
    /// <param name="input">the url in string format</param>
    /// <returns>parsed object</returns>
    /// <exception cref="ArgumentException">if the input url is not valid</exception>
    public static NXMUrl Parse(string input)
    {
        var parsed = new Uri(input);

        if (parsed.Scheme != "nxm")
        {
            throw new ArgumentException($"invalid url \"{input}\"");
        }

        NXMUrl? result = null;

        if (parsed.Host == "oauth")
        {
            result = new NXMOAuthUrl(parsed);
        }
        else if (parsed.Segments.Length >= 5)
        {
            if (parsed.Segments[1] == "mods/")
            {
                result = new NXMModUrl(parsed);
            }
            else if (parsed.Segments[1] == "collections/")
            {
                result = new NXMCollectionUrl(parsed);
            }
        }

        if (result == null)
        {
            throw new ArgumentException($"invalid url \"{input}\"");
        }

        result._query = System.Web.HttpUtility.ParseQueryString(parsed.Query);
        return result;
    }

    /// <summary>
    /// the (re-)assembled query string of the url
    /// </summary>
    protected string QueryString
    {
        get
        {
            return string.Join("&", _query.AllKeys.Select(key => $"{key}={_query[key]}"));
        }
    }

    /// <summary>
    /// safe cast to the specialized mod url type
    /// </summary>
    public NXMModUrl Mod
    {
        get
        {
            if (UrlType != NXMUrlType.Mod)
            {
                throw new ArgumentException("not a mod url");
            }
            return (NXMModUrl)this;
        }
    }

    /// <summary>
    /// safe cast to the specialized collection url type
    /// </summary>
    public NXMCollectionUrl Collection
    {
        get
        {
            if (UrlType != NXMUrlType.Collection)
            {
                throw new ArgumentException("not a collection url");
            }
            return (NXMCollectionUrl)this;
        }
    }

    /// <summary>
    /// safe cast to the specialized oauth callback type
    /// </summary>
    public NXMOAuthUrl OAuth
    {
        get
        {
            if (UrlType != NXMUrlType.OAuth)
            {
                throw new ArgumentException("not an oauth callback url");
            }
            return (NXMOAuthUrl)this;
        }
    }

}

/// <summary>
/// specialized variant of NXMUrl for mod urls
/// </summary>
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
    public GameDomain Game { get; set; }

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="uri">parsed uri</param>
    /// <exception cref="ArgumentException">if the uri is not valid</exception>
    public NXMModUrl(Uri uri)
    {
        UrlType = NXMUrlType.Mod;
        if ((uri.Segments.Length != 5) || (uri.Segments[3] != "files/"))
        {
            throw new ArgumentException($"invalid nxm url \"{uri}\"");
        }
        Game = GameDomain.From(uri.Host);
        try
        {
            ModId = ModId.From(ulong.Parse(uri.Segments[2].TrimEnd('/')));
            FileId = FileId.From(ulong.Parse(uri.Segments[4].TrimEnd('/')));
        } catch (FormatException)
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

/// <summary>
/// specialized variant of NXMUrl for collecton links
/// </summary>
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
    public GameDomain Game { get; set; }

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="uri">parsed uri</param>
    /// <exception cref="ArgumentException">if the uri is not valid</exception>
    public NXMCollectionUrl(Uri uri)
    {
        UrlType = NXMUrlType.Collection;
        if ((uri.Segments.Length != 5) || (uri.Segments[3] != "revisions/"))
        {
            throw new ArgumentException($"invalid nxm url \"{uri}\"");
        }
        Game = GameDomain.From(uri.Host);
        try
        {
            Slug = CollectionSlug.From(uri.Segments[2].TrimEnd('/'));
            Revision = RevisionNumber.From(ulong.Parse(uri.Segments[4].TrimEnd('/')));
        } catch (FormatException)
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

/// <summary>
/// specialized variant of NXMUrl for oauth callback links
/// </summary>
public class NXMOAuthUrl : NXMUrl
{
    /// <summary>
    /// a code. This gets used in another request to get at the actual authorization code
    /// </summary>
    public string? Code
    {
        get
        {
            return _query?.Get("code");
        }
    }

    /// <summary>
    /// this is a random id we generated when initially creating the request, it allows us to
    /// match the respons to the initial request we made
    /// </summary>
    public string? State
    {
        get
        {
            return _query?.Get("state");
        }
    }

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="uri">parsed url</param>
    public NXMOAuthUrl(Uri uri)
    {
        UrlType = NXMUrlType.OAuth;
        if ((uri.Segments.Length != 2) || (uri.Segments[1] != "callback"))
        {
            throw new ArgumentException($"invalid nxm url \"{uri}\"");
        }
    }

    /// <summary>
    /// serialize the url
    /// </summary>
    public override string ToString()
    {
        return $"nxm://oauth/callback?{QueryString}";
    }
}
