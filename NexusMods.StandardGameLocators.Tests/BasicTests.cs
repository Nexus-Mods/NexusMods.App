using Microsoft.Extensions.Logging;
using NexusMods.Interfaces.Components;

namespace NexusMods.StandardGameLocators.Tests;

public class BasicTests
{
    private readonly IGame _game;
    private readonly ILogger<BasicTests> _logger;

    public BasicTests(ILogger<BasicTests> logger, StubbedGame game)
    {
        _game = game;
        _logger = logger;
    }
    
    [Fact]
    public void CanFindGames()
    {
        Assert.NotEmpty(_game.Installations);
    }
}