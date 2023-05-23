using FluentAssertions;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.CLI.Tests.VerbTests;

public class ListGames : AVerbTest
{
    public ListGames(TemporaryFileManager temporaryFileManager, IServiceProvider provider) : base(temporaryFileManager, provider)
    {
    }

    [Fact]
    public async Task CanListGames()
    {
        await RunNoBannerAsync("--noBanner", "list-games");

        LogSize.Should().Be(1);
        LastTable.Rows.First().OfType<IGame>().First().Name.Should().Be("Stubbed Game");
    }
}
