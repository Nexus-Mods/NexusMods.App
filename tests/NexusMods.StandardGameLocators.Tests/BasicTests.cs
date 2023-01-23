using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Interfaces;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.StandardGameLocators.Tests;

public class BasicTests
{
    private readonly IGame _game;
    private readonly ILogger<BasicTests> _logger;
    private readonly GameInstallation _steamInstall;
    private readonly GameInstallation? _gogInstall;

    public BasicTests(ILogger<BasicTests> logger, StubbedGame game)
    {
        _game = game;
        _logger = logger;
        _steamInstall = _game.Installations.First(g => g.Locations.Any(l => l.ToString().Contains("steam_game")));
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
            _gogInstall = _game.Installations.First(g => g.Locations.Any(l => l.ToString().Contains("gog_game")));

    }
    
    [Fact]
    public void CanFindGames()
    {
        Assert.NotEmpty(_game.Installations);
    }

    [Fact]
    public void CanGetInstallLocations()
    {
        _steamInstall!.Locations[GameFolderType.Game].Join("StubbedGame.exe").FileExists.Should().BeTrue();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _gogInstall!.Locations[GameFolderType.Game].Join("StubbedGame.exe").FileExists.Should().BeTrue();
        }
    }

    [Fact]
    public void CanConvertToString()
    {
        Assert.Equal("Stubbed Game v0.0.0.0", _steamInstall.ToString());
    }
}