using FluentAssertions;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Networking.NexusWebApi.V1Interop;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Networking.NexusWebApi.Tests;

[Trait("RequiresNetworking", "True")]
public class GameDomainToGameIdMappingCacheTests(GameDomainToGameIdMappingCache cache)
{
    [Theory]
    [MemberData(nameof(TryGetDomainAsyncTestData))]
    public async Task TryGetDomainAsync_WithMissingMapping_ShouldResolveMapping(NexusModsGameId nexusModsGameId, GameDomain expectedGameDomain)
    {
        // Act
        var result = await cache.TryGetDomainAsync(nexusModsGameId, CancellationToken.None);

        // Assert
        result.Value.Should().Be(expectedGameDomain);
    }
    
    [Theory]
    [MemberData(nameof(TryGetDomainAsyncTestData))]
    public void TryGetDomain_WithMissingMapping_ShouldResolveMapping(NexusModsGameId nexusModsGameId, GameDomain expectedGameDomain)
    {
        // Act
        var result = cache.TryGetDomain(nexusModsGameId, CancellationToken.None);

        // Assert
        result.Value.Should().Be(expectedGameDomain);
    }
    
    [Theory]
    [MemberData(nameof(TryGetIdAsyncTestData))]
    public async Task TryGetIdAsync_WithMissingMapping_ShouldResolveMapping(GameDomain gameDomain, NexusModsGameId expectedNexusModsGameId)
    {
        // Act
        var result = await cache.TryGetIdAsync(gameDomain, CancellationToken.None);

        // Assert
        result.Value.Should().Be(expectedNexusModsGameId);
    }
    
    [Theory]
    [MemberData(nameof(TryGetIdAsyncTestData))]
    public void TryGetId_WithMissingMapping_ShouldResolveMapping(GameDomain gameDomain, NexusModsGameId expectedNexusModsGameId)
    {
        // Act
        var result = cache.TryGetId(gameDomain, CancellationToken.None);

        // Assert
        result.Value.Should().Be(expectedNexusModsGameId);
    }
    
    private static readonly (GameDomain Domain, NexusModsGameId Id)[] KnownMappings =
    [
        (GameDomain.From("stardewvalley"), NexusModsGameId.From(1303)),
        (GameDomain.From("cyberpunk2077"), NexusModsGameId.From(3333)),
        (GameDomain.From("baldursgate3"), NexusModsGameId.From(3474)),
        (GameDomain.From("site"), NexusModsGameId.From(2295)),
    ];
    
    public static IEnumerable<object[]> TryGetIdAsyncTestData()
    {
        foreach (var (domain, id) in KnownMappings)
        {
            yield return [domain, id];
        }
    }

    public static IEnumerable<object[]> TryGetDomainAsyncTestData()
    {
        foreach (var (domain, id) in KnownMappings)
        {
            yield return [id, domain];
        }
    }
}
