using System.Net;
using FluentAssertions;

namespace NexusMods.Networking.NexusWebApi.Tests;

[Trait("RequiresNetworking", "True")]
public class ClientTests
{
    private readonly Client _client;

    public ClientTests(Client client)
    {
        _client = client;
    }

    [Fact]
    public async Task CanGetGames()
    {
        var games = await _client.Games();

        games.StatusCode.Should().Be(HttpStatusCode.OK);
        games.Data.Should().NotBeEmpty();
        games.Data.Select(g => g.Name).Should().Contain("Skyrim Special Edition");
        games.Data.Length.Should().BeGreaterThan(2000);
    }
}
