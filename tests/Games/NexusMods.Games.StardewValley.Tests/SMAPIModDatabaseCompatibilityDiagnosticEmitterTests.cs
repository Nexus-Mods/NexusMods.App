using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.StardewValley.Emitters;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace NexusMods.Games.StardewValley.Tests;

public class SMAPIModDatabaseCompatibilityDiagnosticEmitterTests : ALoadoutDiagnosticEmitterTest<SMAPIModDatabaseCompatibilityDiagnosticEmitterTests, StardewValley, SMAPIModDatabaseCompatibilityDiagnosticEmitter>
{
    public SMAPIModDatabaseCompatibilityDiagnosticEmitterTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddStardewValley()
            .AddUniversalGameLocator<StardewValley>(new Version("1.6.14"));
    }

    [SkippableFact]
    [Trait("RequiresApiKey", "True")]
    public async Task Test_ModCompatabilityObsolete()
    {
        ApiKeyTestHelper.SkipIfApiKeyNotAvailable();
        var loadout = await CreateLoadout();

        // SMAPI 4.1.10 (https://www.nexusmods.com/stardewvalley/mods/2400?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(2400), FileId.From(119630));

        // Extra Map Layers 0.3.10 (https://www.nexusmods.com/stardewvalley/mods/9633?tab=files)
        var extraMapLayers = await InstallModFromNexusMods(loadout, ModId.From(9633), FileId.From(75206));

        var diagnostic = await GetSingleDiagnostic(loadout);
        var modCompatabilityObsoleteMessageData = diagnostic.Should().BeOfType<Diagnostic<Diagnostics.ModCompatabilityObsoleteMessageData>>(because: "Extra Map Layers is obsolete in 1.6").Which.MessageData;

        var mod = modCompatabilityObsoleteMessageData.SMAPIMod.ResolveData(ServiceProvider, Connection);

        mod.LoadoutItemGroupId.Should().Be(extraMapLayers.LoadoutItemGroupId);
        modCompatabilityObsoleteMessageData.ReasonPhrase.Should().Be("extra map layer support was added in Stardew Valley 1.6. You can delete this mod.");

        await VerifyDiagnostic(diagnostic);
    }

    [SkippableFact]
    [Trait("RequiresApiKey", "True")]
    public async Task Test_ModCompatabilityAssumeBroken()
    {
        ApiKeyTestHelper.SkipIfApiKeyNotAvailable();
        var loadout = await CreateLoadout();

        // SMAPI 4.1.10 (https://www.nexusmods.com/stardewvalley/mods/2400?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(2400), FileId.From(119630));

        // Persistent Mines 1.0.1 (https://www.nexusmods.com/stardewvalley/mods/14985?tab=files)
        var persistentMines = await InstallModFromNexusMods(loadout, ModId.From(14985), FileId.From(64850));

        var diagnostic = await GetSingleDiagnostic(loadout);
        var modCompatabilityAssumeBrokenMessageData = diagnostic.Should().BeOfType<Diagnostic<Diagnostics.ModCompatabilityAssumeBrokenMessageData>>(because: "").Which.MessageData;

        var mod = modCompatabilityAssumeBrokenMessageData.SMAPIMod.ResolveData(ServiceProvider, Connection);

        mod.LoadoutItemGroupId.Should().Be(persistentMines.LoadoutItemGroupId);
        modCompatabilityAssumeBrokenMessageData.ReasonPhrase.Should().Be("affected by breaking changes in the SpaceCore mod API");
        modCompatabilityAssumeBrokenMessageData.ModVersion.Should().Be("1.0.1");

        await VerifyDiagnostic(diagnostic);
    }
}
