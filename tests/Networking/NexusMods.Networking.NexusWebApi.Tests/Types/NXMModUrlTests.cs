using Xunit;
using NexusMods.Networking.NexusWebApi.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NexusMods.DataModel.Games;

namespace NexusMods.Networking.NexusWebApi.Types.Tests;

public class NXMModUrlTests
{
    [Fact()]
    public void CanParseBasicModUrls()
    {
        var parsed = NXMUrl.Parse("nxm://skyrim/mods/123/files/456");
        Assert.Equal(NXMUrlType.Mod, parsed.UrlType);
        Assert.Equal(GameDomain.From("skyrim"), parsed.Mod.Game);
        Assert.Equal(123u, parsed.Mod.ModId.Value);
        Assert.Equal(456u, parsed.Mod.FileId.Value);
        Assert.Null(parsed.Key);
        Assert.Null(parsed.ExpireTime);
        Assert.Null(parsed.User);
    }

    [Fact()]
    public void CanParsePersonalizedModUrls()
    {
        var parsed = NXMUrl.Parse("nxm://skyrim/mods/123/files/456?key=key&expires=1676541088&user_id=12345");
        Assert.Equal("key", parsed.Key?.Value);
        Assert.NotNull(parsed.ExpireTime);
        Assert.Equal(DateTime.UnixEpoch.AddSeconds(1676541088), parsed.ExpireTime.Value);
        Assert.NotNull(parsed.User);
        Assert.Equal(12345u, parsed.User.Value.Value);
    }

    [Fact()]
    public void CanParseCollectionUrls()
    {
        var parsed = NXMUrl.Parse("nxm://skyrim/collections/slug/revisions/42");
        Assert.Equal(NXMUrlType.Collection, parsed.UrlType);
        Assert.Equal(GameDomain.From("skyrim"), parsed.Collection.Game);
        Assert.Equal("slug", parsed.Collection.Slug.Value);
        Assert.Equal(42u, parsed.Collection.Revision.Value);
    }

    [Fact()]
    public void CanParseOAuthUrls()
    {
        var parsed = NXMUrl.Parse("nxm://oauth/callback?code=code&state=state");
        Assert.Equal(NXMUrlType.OAuth, parsed.UrlType);
        Assert.Equal("code", parsed.OAuth.Code);
        Assert.Equal("state", parsed.OAuth.State);
    }

    [Fact()]
    public void ToleratesUnknownParameters()
    {
        var parsed = NXMUrl.Parse("nxm://skyrim/mods/123/files/456?whatdis=value");
        Assert.NotNull(parsed);
    }

    [Fact()]
    public void RejectsInvalidUrls()
    {
        Assert.Throws<ArgumentException>(() => NXMUrl.Parse("notnxm://skyrim/mods/123/files/456"));
        Assert.Throws<ArgumentException>(() => NXMUrl.Parse("nxm://skyrim/invalid/123/files/456"));
        Assert.Throws<ArgumentException>(() => NXMUrl.Parse("nxm://skyrim/mods/notanumber/files/456"));
        Assert.Throws<ArgumentException>(() => NXMUrl.Parse("nxm://skyrim/mods/123/invalid/456"));
        Assert.Throws<ArgumentException>(() => NXMUrl.Parse("nxm://skyrim/mods/123/files/notanumber"));
        Assert.Throws<ArgumentException>(() => NXMUrl.Parse("nxm://skyrim/mods/123/files"));
        Assert.Throws<ArgumentException>(() => NXMUrl.Parse("nxm://skyrim/mods/123/files/456/toomuch"));
    }
}