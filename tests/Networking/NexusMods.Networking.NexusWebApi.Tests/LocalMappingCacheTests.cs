using FluentAssertions;
using NexusMods.Networking.NexusWebApi.V1Interop;

namespace NexusMods.Networking.NexusWebApi.Tests;

public class LocalMappingCacheTests
{
    [Fact]
    public void Test_Parse()
    {
        LocalMappingCache.TryParseJsonFile(out _, out _).Should().BeTrue();
    }
}
