using FluentAssertions;
using NexusMods.CLI.DataOutputs;
using NexusMods.Interfaces.Components;

namespace NexusMods.CLI.Tests;

public class CLITests
{
    private readonly CommandLineBuilder _builder;
    private readonly LoggingRenderer _renderer;

    public CLITests(CommandLineBuilder builder, LoggingRenderer renderer)
    {
        _builder = builder;
        _renderer = renderer;
    }
    
    [Fact]
    public async Task CanListGames()
    {
        _renderer.Reset();
        Assert.Equal(0, await _builder.Run(new[] {"--noBanner", "list-games" }));
        
        Assert.Equal(1, _renderer.Logged.Count);

        _renderer.Logged.First().Should().BeAssignableTo<Table>();
        var row = _renderer.Logged.OfType<Table>().First().Rows.First();
        row.OfType<IGame>().First().Name.Should().Be("Stubbed Game");

    }
}