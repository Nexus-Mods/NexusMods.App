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
        Assert.NotNull(info);
    }
    
}
