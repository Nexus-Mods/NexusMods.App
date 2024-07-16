using System.Reactive.Linq;
using FluentAssertions;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.RedEngine.Tests;

public class Cyberpunk2077DiagnosticTests(IServiceProvider serviceProvider) : AGameTest<Cyberpunk2077.Cyberpunk2077Game>(serviceProvider)
{
    public static readonly string Source = "NexusMods.Games.RedEngine.Cyberpunk2077";
    
    [Fact]
    public async Task Red4ExtMissingDiagnostic()
    {
        // Install a mod that needs Red4Ext, but Red4Ext is missing.
        var loadout = await CreateLoadout();
        {
            using var tx = Connection.BeginTransaction();
            var pluginMod = AddEmptyMod(tx, loadout, "PluginMod");
            AddFile(tx, loadout, pluginMod, new GamePath(LocationId.Game, "red4ext/plugins/PinkCyberware/pluginFile.dll"));

            await tx.Commit();
        }

        Refresh(ref loadout);
        
        // The diagnostic should be emitted.
        var diagnostics = await DiagnosticManager.Run(loadout);
        diagnostics.Should().ContainSingle(d => d.Id == Red4ExtMissingEmitter.Id);
        
        Abstractions.Loadouts.Mods.ModId red4ExtModId = default;
        
        // Install Red4Ext and the diagnostic should disappear.
        {
            using var tx = Connection.BeginTransaction();
            red4ExtModId = AddEmptyMod(tx, loadout, "Red4ExtMod");
            AddFile(tx, loadout, red4ExtModId, new GamePath(LocationId.Game, "red4ext/red4ext.dll"));
            AddFile(tx, loadout, red4ExtModId, new GamePath(LocationId.Game, "bin/x64/winmm.dll"));

            var results = await tx.Commit();
            
            red4ExtModId = results[red4ExtModId];
        }
        
        Refresh(ref loadout);
        
        diagnostics = await DiagnosticManager.Run(loadout);
        diagnostics.Should().BeEmpty();
        
        // Disable Red4Ext and the diagnostic should reappear.
        {
            var red4ExtMod = Mod.Load(Connection.Db, red4ExtModId);

            await red4ExtMod.ToggleEnabled();
        }
        
        Refresh(ref loadout);
        
        diagnostics = await DiagnosticManager.Run(loadout);
        diagnostics.Should().ContainSingle(d => d.Id == Red4ExtMissingEmitter.Id);
    }
}
