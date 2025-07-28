using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NexusMods.Networking.NexusWebApi.V1Interop;

namespace NexusMods.Networking.NexusWebApi.Tests;

public class LocalMappingCacheTests
{
    [Fact]
    public void Test_Parse()
    {
        LocalMappingCache.TryParseJsonFile(NullLogger.Instance, out _, out _).Should().BeTrue();
    }
}
