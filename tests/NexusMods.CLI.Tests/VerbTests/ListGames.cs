using FluentAssertions;
using NexusMods.DataModel.Games;
using NexusMods.ProxyConsole.Abstractions.Implementations;

namespace NexusMods.CLI.Tests.VerbTests;

public class ListGames(IServiceProvider provider) : AVerbTest(provider)
{
    [Fact]
    public async Task CanListGames()
    {
        var log = await Run("list-games");
        log.Size.Should().Be(1);
        log.TableCellsWith("Stubbed Game").Should().NotBeEmpty();
    }
}
