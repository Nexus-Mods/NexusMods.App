using Microsoft.Extensions.Logging;

namespace NexusMods.Games.BethesdaGameStudios.Tests;

[Trait("RequiresInstalls", "True")]
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
        Assert.Equal("Skyrim Special Edition", _game.Name);
        Assert.Equal("skyrimspecialedition", _game.Slug);
        Assert.Equal(2, _game.Installations.Count());
    }
}