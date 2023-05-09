using FluentAssertions;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators.Tests;

public class BasicTests
{
    private readonly IGame _game;

    public BasicTests(IGame game)
    {
        _game = game;
    }

    [SkippableFact]
    public void Test_Locators_Linux()
    {
        Skip.If(!OperatingSystem.IsLinux());

        _game.Installations.Should().SatisfyRespectively(
            steamInstallation =>
            {
                steamInstallation.Locations.Should().ContainSingle(kv =>
                    kv.Key == GameFolderType.Game && kv.Value.ToString().Contains("steam_game"));
            });
    }

    [SkippableFact]
    public void Test_Locators_Windows()
    {
        Skip.If(!OperatingSystem.IsWindows());

        _game.Installations.Should().SatisfyRespectively(
            eaInstallation =>
            {
                eaInstallation.Locations.Should().ContainSingle(kv =>
                    kv.Key == GameFolderType.Game && kv.Value.ToString().Contains("ea_game"));
            },
            epicInstallation =>
            {
                epicInstallation.Locations.Should().ContainSingle(kv =>
                    kv.Key == GameFolderType.Game && kv.Value.ToString().Contains("epic_game"));
            },
            originInstallation =>
            {
                originInstallation.Locations.Should().ContainSingle(kv =>
                    kv.Key == GameFolderType.Game && kv.Value.ToString().Contains("origin_game"));
            },
            gogInstallation =>
            {
                gogInstallation.Locations.Should().ContainSingle(kv =>
                    kv.Key == GameFolderType.Game && kv.Value.ToString().Contains("gog_game"));
            },
            steamInstallation =>
            {
                steamInstallation.Locations.Should().ContainSingle(kv =>
                    kv.Key == GameFolderType.Game && kv.Value.ToString().Contains("steam_game"));
            },
            xboxInstallation =>
            {
                xboxInstallation.Locations.Should().ContainSingle(kv =>
                    kv.Key == GameFolderType.Game && kv.Value.ToString().Contains("xbox_game"));
            });
    }

    [Fact]
    public void CanFindGames()
    {
        _game.Installations.Should().NotBeEmpty();
    }
}
