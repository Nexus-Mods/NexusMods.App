using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NexusMods.Interfaces;
using NexusMods.Interfaces.Components;
using NexusMods.Paths;

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
        
        Assert.Equal(@"c:\games\steam_game\1", _steamInstall!.Locations[GameFolderType.Game].ToString());
        Assert.Equal(Version.Parse("0.0.1.0"), _steamInstall.Version);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Equal(@"c:\games\gog_game\1", _gogInstall!.Locations[GameFolderType.Game].ToString());
            Assert.Equal(Version.Parse("0.0.1.0"), _gogInstall.Version);
        }
    }

    [Fact]
    public void CanConvertToString()
    {
        Assert.Equal("Stubbed Game v0.0.1.0", _steamInstall.ToString());
    }
}