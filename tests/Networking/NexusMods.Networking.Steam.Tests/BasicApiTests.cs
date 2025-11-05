using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NexusMods.Sdk.Hashes;
using NexusMods.Abstractions.Steam;
using NexusMods.Abstractions.Steam.Values;
using Xunit;

namespace NexusMods.Networking.Steam.Tests;

public class BasicApiTests(ILogger<BasicApiTests> logger, ISteamSession session)
{
    private static readonly AppId SdvAppId = AppId.From(413150);

    [Fact(Skip = "Requires Human Interaction")]
    public async Task CanGetProductInfo()
    {
        var info = await session.GetProductInfoAsync(SdvAppId);
        info.Depots.Should().HaveCountGreaterOrEqualTo(3, "SDV has one depot for each major OS");

        var depot = info.Depots.First(d => d.OsList.Contains("windows"));
        var manifest = await session.GetManifestContents(SdvAppId, depot.DepotId, depot.Manifests["public"].ManifestId, "public");
        
        var largestFile = manifest.Files.OrderByDescending(f => f.Size).First();
        await using var stream = session.GetFileStream(SdvAppId, manifest, largestFile.Path);

        var result = await Sha1Hasher.HashAsync(stream);
        stream.Length.Should().Be((long)largestFile.Size.Value);

        result.Should().Be(largestFile.Hash);
    }
    
}
