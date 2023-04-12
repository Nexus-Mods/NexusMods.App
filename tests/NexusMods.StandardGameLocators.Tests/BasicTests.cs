using System.Runtime.InteropServices;
using FluentAssertions;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

namespace NexusMods.StandardGameLocators.Tests;

public class BasicTests
{
    private readonly IGame _game;
    private readonly GameInstallation _steamInstall;
    private readonly GameInstallation? _gogInstall;

    public BasicTests(StubbedGame game)
    {
        _game = game;
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
        _steamInstall.Locations[GameFolderType.Game].CombineUnchecked("StubbedGame.exe").FileExists.Should().BeTrue();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _gogInstall!.Locations[GameFolderType.Game].CombineUnchecked("StubbedGame.exe").FileExists.Should().BeTrue();
        }
    }

    [Fact]
    public void CanConvertToString()
    {
        Assert.Equal("Stubbed Game v0.0.0.0 (Unknown)", _steamInstall.ToString());
    }
}
