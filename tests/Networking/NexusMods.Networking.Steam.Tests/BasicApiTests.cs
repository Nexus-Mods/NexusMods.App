using FluentAssertions;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Steam;
using NexusMods.Abstractions.Steam.Values;
using Xunit;

namespace NexusMods.Networking.Steam.Tests;

public class BasicApiTests(ILogger<BasicApiTests> logger, ISteamSession session)
{
    private static readonly AppId SdvAppId = AppId.From(413150);

    [Fact]
    public async Task CanGetProductInfo()
    {
        var info = await session.GetProductInfoAsync(SdvAppId);
        info.Depots.Should().HaveCountGreaterOrEqualTo(3, "SDV has one depot for each major OS");

        var depot = info.Depots.First(d => d.OsList.Contains("windows"));
        var manifest = await session.GetManifestContents(SdvAppId, depot.DepotId, depot.Manifests["public"].ManifestId, "public");
        
        manifest.Should().NotBeNull();
    }
    
}
