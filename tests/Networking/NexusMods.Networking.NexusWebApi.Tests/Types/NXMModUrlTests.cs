using NexusMods.DataModel.Games;
using FluentAssertions;

namespace NexusMods.Networking.NexusWebApi.Types.Tests;

public class NXMModUrlTests
{
    [Fact()]
    public void CanParseBasicModUrls()
    {
        var parsed = NXMUrl.Parse("nxm://skyrim/mods/123/files/456");
        parsed.UrlType.Should().Be(NXMUrlType.Mod);
        parsed.Mod.Game.Should().Be(GameDomain.From("skyrim"));
        parsed.Mod.ModId.Value.Should().Be(123u);
        parsed.Mod.FileId.Value.Should().Be(456u);
        parsed.Key.Should().BeNull();
        parsed.ExpireTime.Should().BeNull();
        parsed.User.Should().BeNull();
    }

    [Fact()]
    public void CanParsePersonalizedModUrls()
    {
        var parsed = NXMUrl.Parse("nxm://skyrim/mods/123/files/456?key=key&expires=1676541088&user_id=12345");
        parsed.Key?.Value.Should().Be("key");
        parsed.ExpireTime.Should().NotBeNull();
        parsed.ExpireTime!.Value.Should().Be(DateTime.UnixEpoch.AddSeconds(1676541088));
        parsed.User.Should().NotBeNull();
        parsed.User!.Value.Value.Should().Be(12345);
    }

    [Fact()]
    public void CanParseCollectionUrls()
    {
        var parsed = NXMUrl.Parse("nxm://skyrim/collections/slug/revisions/42");
        parsed.UrlType.Should().Be(NXMUrlType.Collection);
        parsed.Collection.Game.Should().Be(GameDomain.From("skyrim"));
        parsed.Collection.Slug.Value.Should().Be("slug");
        parsed.Collection.Revision.Value.Should().Be(42u);
    }

    [Fact()]
    public void CanParseOAuthUrls()
    {
        var parsed = NXMUrl.Parse("nxm://oauth/callback?code=code&state=state");
        parsed.UrlType.Should().Be(NXMUrlType.OAuth);
        parsed.OAuth.Code.Should().Be("code");
        parsed.OAuth.State.Should().Be("state");
    }

    [Fact()]
    public void ToleratesUnknownParameters()
    {
        var parsed = NXMUrl.Parse("nxm://skyrim/mods/123/files/456?whatdis=value");
        parsed.Should().NotBeNull();
    }

    [Theory()]
    [InlineData("notnxm://skyrim/mods/123/files/456")]
    [InlineData("nxm://skyrim/invalid/123/files/456")]
    [InlineData("nxm://skyrim/mods/notanumber/files/456")]
    [InlineData("nxm://skyrim/mods/123/invalid/456")]
    [InlineData("nxm://skyrim/mods/123/files/notanumber")]
    [InlineData("nxm://skyrim/mods/123/files")]
    [InlineData("nxm://skyrim/mods/123/files/456/toomuch")]
    public void RejectsInvalidUrls(string url)
    {
        url.Invoking(NXMUrl.Parse).Should().Throw<ArgumentException>();
    }
}