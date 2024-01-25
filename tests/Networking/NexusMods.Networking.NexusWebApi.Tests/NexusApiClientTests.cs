using System.Net;
using FluentAssertions;

namespace NexusMods.Networking.NexusWebApi.Tests;

[Trait("RequiresNetworking", "True")]
public class NexusApiClientTests
{
    private readonly NexusApiClient _nexusApiClient;

    public NexusApiClientTests(NexusApiClient nexusApiClient)
    {
        _nexusApiClient = nexusApiClient;
    }

    [Fact]
    public async Task CanGetGames()
    {
        var games = await _nexusApiClient.Games();

        games.StatusCode.Should().Be(HttpStatusCode.OK);
        games.Data.Should().NotBeEmpty();
        games.Data.Select(g => g.Name).Should().Contain("Skyrim Special Edition");
        games.Data.Length.Should().BeGreaterThan(2000);
    }
}
