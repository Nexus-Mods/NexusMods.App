using FluentAssertions;
using NexusMods.CLI.DataOutputs;
using NexusMods.CLI.Tests.VerbTests;
using NexusMods.Interfaces.Components;
using NexusMods.Paths;

namespace NexusMods.CLI.Tests;

public class ListGames : AVerbTest 
{ 
    public ListGames(TemporaryFileManager temporaryFileManager, IServiceProvider provider) : base(temporaryFileManager, provider)
    {
    }

    
    [Fact]
    public async Task CanListGames()
    {
        await RunNoBanner(new[] { "--noBanner", "list-games" });

        LogSize.Should().Be(1);
        LastTable.Rows.First().OfType<IGame>().First().Name.Should().Be("Stubbed Game");

    }

}