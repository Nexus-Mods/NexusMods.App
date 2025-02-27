using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.StardewValley.Emitters;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;

namespace NexusMods.Games.StardewValley.Tests;

[Trait("RequiresNetworking", "True")]
public class MissingSMAPIEmitterTests : ALoadoutDiagnosticEmitterTest<DependencyDiagnosticEmitterTests, StardewValley, MissingSMAPIEmitter>
{
    public MissingSMAPIEmitterTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddStardewValley()
            .AddUniversalGameLocator<StardewValley>(new Version("1.6.14"));
    }

    [Fact]
    public async Task Test_SMAPIRequiredButNotInstalled()
    {
        var loadout = await CreateLoadout();

        // Content Patcher 2.5.3 (https://www.nexusmods.com/stardewvalley/mods/1915?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(1915), FileId.From(124659));

        // Farm Type Manager 1.24.0 (https://www.nexusmods.com/stardewvalley/mods/3231?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(3231), FileId.From(117244));

        var diagnostic = await GetSingleDiagnostic(loadout);
        var smapiRequiredButNotInstalledMessageData = diagnostic.Should().BeOfType<Diagnostic<Diagnostics.SMAPIRequiredButNotInstalledMessageData>>(because: "SMAPI isn't installed but required by 2 mods").Which.MessageData;

        smapiRequiredButNotInstalledMessageData.ModCount.Should().Be(2, because: "the loadout contains 2 SMAPI mods");

        // SMAPI 4.1.10 (https://www.nexusmods.com/stardewvalley/mods/2400?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(2400), FileId.From(119630));

        await ShouldHaveNoDiagnostics(loadout, because: "SMAPI is now installed");

        await VerifyDiagnostic(diagnostic);
    }

    [Fact]
    public async Task Test_SMAPIRequiredButDisabled()
    {
        var loadout = await CreateLoadout();

        // Content Patcher 2.5.3 (https://www.nexusmods.com/stardewvalley/mods/1915?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(1915), FileId.From(124659));

        // Farm Type Manager 1.24.0 (https://www.nexusmods.com/stardewvalley/mods/3231?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(3231), FileId.From(117244));

        // SMAPI 4.1.10 (https://www.nexusmods.com/stardewvalley/mods/2400?tab=files)
        var smapi = await InstallModFromNexusMods(loadout, ModId.From(2400), FileId.From(119630));

        await ShouldHaveNoDiagnostics(loadout, because: "SMAPI is installed and enabled");

        await DisabledMod(smapi);

        var diagnostic = await GetSingleDiagnostic(loadout);
        var smapiRequiredButDisabledMessageData = diagnostic.Should().BeOfType<Diagnostic<Diagnostics.SMAPIRequiredButDisabledMessageData>>(because: "SMAPI is required for 2 mods but disabled").Which.MessageData;

        smapiRequiredButDisabledMessageData.ModCount.Should().Be(2, because: "the loadout contains 2 SMAPI mods");

        await VerifyDiagnostic(diagnostic);
    }
}
