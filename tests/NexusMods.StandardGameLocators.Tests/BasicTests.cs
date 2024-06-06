using System.Runtime.Versioning;
using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;

namespace NexusMods.StandardGameLocators.Tests;

public class BasicTests(IGameRegistry registry)
{

    [SkippableFact]
    [SupportedOSPlatform("linux")]

    public void Test_Locators_Linux()
    {
        Skip.If(!OperatingSystem.IsLinux());

        registry.Installations.Values.Should().SatisfyRespectively(
            steamInstallation =>
            {
                steamInstallation.LocationsRegister.LocationDescriptors
                    .Where(d => d.Value.Id == LocationId.Game)
                    .Should().ContainSingle()
                    .Which.Value.ResolvedPath
                    .ToString().Should().Contain("steam_game");
            });
    }

    [SkippableFact]
    [SupportedOSPlatform("windows")]

    public void Test_Locators_Windows()
    {
        Skip.If(!OperatingSystem.IsWindows());

        // TODO: Enable XboxGamePass back again when it's supported 
        registry.Installations.Values
            .Should().HaveCount(5)
            .And.Satisfy(
                eaInstallation => eaInstallation.Store == GameStore.EADesktop,
                epicInstallation => epicInstallation.Store == GameStore.EGS,
                originInstallation => originInstallation.Store == GameStore.Origin,
                gogInstallation => gogInstallation.Store == GameStore.GOG,
                steamInstallation => steamInstallation.Store == GameStore.Steam
                // xboxInstallation => xboxInstallation.Store == GameStore.XboxGamePass
            );
    }

    [Fact]
    public void CanFindGames()
    {
        registry.Installations.Values.Should().NotBeEmpty();
    }
}
