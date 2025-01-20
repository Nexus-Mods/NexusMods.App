using System.Collections.Specialized;

namespace NexusMods.Abstractions.NexusWebApi.Types;

/// <summary>
/// parsed url of nxm:// protocol, used in multiple ways for the site communicating with clients
/// </summary>
// ReSharper disable once InconsistentNaming
public class NXMUrl
{
    /// <summary>
    /// url type
    /// </summary>
    public NXMUrlType UrlType { get; protected init; }

    /// <summary>
    /// if applicable, the time the url becomes invalid
    /// </summary>
    public DateTime? ExpireTime
    {
        get
        {
            var expires = Query.Get("expires");
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
            var user = Query.Get("user_id");
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
            var key = Query.Get("key");
            return key != null ? NXMKey.From(key) : null;
        }
    }

    /// <summary>
    /// parsed query arguments in the url (everything after the ? in the url)
    /// </summary>
    protected NameValueCollection Query = new();


    /// <summary>
    /// parse a nxm:// url from a string
    /// </summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    public static NXMUrl Parse(string uri)
    {
        return Parse(new Uri(uri));
    }

    /// <summary>
    /// parse a nxm:// url
    /// </summary>
    /// <param name="uri">the url in string format</param>
    /// <returns>parsed object</returns>
    /// <exception cref="ArgumentException">if the input url is not valid</exception>
    public static NXMUrl Parse(Uri uri)
    {
        if (uri.Scheme != "nxm")
        {
            throw new ArgumentException($"invalid url \"{uri}\"");
        }

        NXMUrl? result = null;

        if (uri.Host == "oauth")
        {
            result = new NXMOAuthUrl(uri);
        }
        else if (uri.Host == "gog-auth")
        {
            result = new NXMGogAuthUrl(uri);
        }
        else if (uri.Segments.Length >= 5)
        {
            if (uri.Segments[1] == "mods/")
            {
                result = new NXMModUrl(uri);
            }
            else if (uri.Segments[1] == "collections/")
            {
                result = new NXMCollectionUrl(uri);
            }
        }

        if (result == null)
        {
            throw new ArgumentException($"invalid url \"{uri}\"");
        }

        result.Query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return result;
    }

    /// <summary>
    /// the (re-)assembled query string of the url
    /// </summary>
    protected string QueryString => string.Join("&", Query.AllKeys.Select(key => $"{key}={Query[key]}"));

    /// <summary>
    /// safe cast to the specialized mod url type
    /// </summary>
    public NXMModUrl Mod
    {
        get
        {
            if (UrlType != NXMUrlType.Mod)
                throw new ArgumentException("not a mod url");

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
                throw new ArgumentException("not a collection url");

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
                throw new ArgumentException("not an oauth callback url");

            return (NXMOAuthUrl)this;
        }
    }
}
