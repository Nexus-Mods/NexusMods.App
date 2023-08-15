using FluentAssertions;
using NexusMods.DataModel.Games;

namespace NexusMods.StandardGameLocators.Tests;

public class BasicTests
{
    private readonly IGame _game;

    public BasicTests(IGame game)
    {
        _game = game;
    }

    [Fact]
    public void Test_Locators_Linux()
    {
        if (!OperatingSystem.IsLinux()) return;

        _game.Installations.Should().SatisfyRespectively(
            steamInstallation =>
            {
                steamInstallation.Locations
                    .Should().ContainSingle()
                    .Which.Value
                    .ToString().Should().Contain("steam_game");
            });
    }

    [Fact]
    public void Test_Locators_Windows()
    {
        if (!OperatingSystem.IsWindows()) return;

        _game.Installations
            .Should().HaveCount(6)
            .And.Satisfy(
                eaInstallation => eaInstallation.Locations.First().Value.ToString().Contains("ea_game"),
                epicInstallation => epicInstallation.Locations.First().Value.ToString().Contains("epic_game"),
                originInstallation => originInstallation.Locations.First().Value.ToString().Contains("origin_game"),
                gogInstallation => gogInstallation.Locations.First().Value.ToString().Contains("gog_game"),
                steamInstallation => steamInstallation.Locations.First().Value.ToString().Contains("steam_game"),
                xboxInstallation => xboxInstallation.Locations.First().Value.ToString().Contains("xbox_game")
            );
    }

    [Fact]
    public void CanFindGames()
    {
        _game.Installations.Should().NotBeEmpty();
    }
}
