using FluentAssertions;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.CLI.Tests.VerbTests;

public class ListGames(IServiceProvider provider) : AVerbTest(provider)
{
    [Fact]
    public async Task CanListGames()
    {
        var log = await Run("list-games");
        log.Size.Should().Be(1);
        log.LastTable.Rows.First().OfType<IGame>().First().Name.Should().Be("Stubbed Game");
    }
}
