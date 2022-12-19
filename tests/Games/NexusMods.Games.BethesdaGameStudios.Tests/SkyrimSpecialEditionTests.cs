using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace NexusMods.Games.BethesdaGameStudios.Tests;

[Trait("RequiresGameInstalls", "True")]
public class SkyrimSpecialEditionTests
{
    private readonly ILogger<SkyrimSpecialEditionTests> _logger;
    private readonly SkyrimSpecialEdition _game;

    public SkyrimSpecialEditionTests(ILogger<SkyrimSpecialEditionTests> logger, SkyrimSpecialEdition game)
    {
        _logger = logger;
        _game = game;
    }
    
    [Fact]
    public void CanFindGames()
    {
        _game.Name.Should().Be("Skyrim Special Edition");
        _game.Slug.Should().Be("skyrimspecialedition");
        _game.Installations.Count().Should().BeGreaterThan(0);
    }
}