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
                steamInstallation.LocationsRegister.LocationDescriptors
                    .Should().ContainSingle()
                    .Which.Value.ResolvedPath
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
                eaInstallation => eaInstallation.LocationsRegister.LocationDescriptors.First().Value.ResolvedPath
                    .ToString().Contains("ea_game"),
                epicInstallation => epicInstallation.LocationsRegister.LocationDescriptors.First().Value.ResolvedPath
                    .ToString().Contains("epic_game"),
                originInstallation => originInstallation.LocationsRegister.LocationDescriptors.First().Value
                    .ResolvedPath.ToString().Contains("origin_game"),
                gogInstallation => gogInstallation.LocationsRegister.LocationDescriptors.First().Value.ResolvedPath
                    .ToString().Contains("gog_game"),
                steamInstallation => steamInstallation.LocationsRegister.LocationDescriptors.First().Value.ResolvedPath
                    .ToString().Contains("steam_game"),
                xboxInstallation => xboxInstallation.LocationsRegister.LocationDescriptors.First().Value.ResolvedPath
                    .ToString().Contains("xbox_game")
            );
    }

    [Fact]
    public void CanFindGames()
    {
        _game.Installations.Should().NotBeEmpty();
    }
}
