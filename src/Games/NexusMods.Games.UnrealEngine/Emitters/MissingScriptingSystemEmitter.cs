using System.Runtime.CompilerServices;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.UnrealEngine;
using NexusMods.Games.UnrealEngine.Interfaces;
using NexusMods.Games.UnrealEngine.Models;

using Diagnostic = NexusMods.Abstractions.Diagnostics.Diagnostic;

namespace NexusMods.Games.UnrealEngine.Emitters;

public class MissingScriptingSystemEmitter : ILoadoutDiagnosticEmitter
{
    public async IAsyncEnumerable<Diagnostic> Diagnose(
        Loadout.ReadOnly loadout,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        
        var luaMods = ScriptingSystemLuaLoadoutItem.All(loadout.Db)
            .Where(l => l.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadout.LoadoutId)
            .ToArray();
        
        var logicMods = UnrealEngineLogicLoadoutItem.All(loadout.Db)
            .Where(l => l.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadout.LoadoutId)
            .ToArray();

        var count = luaMods.Length + logicMods.Length;
        if (count == 0) yield break;

        var found = Utils.TryGetScriptingSystemLoadoutGroup(loadout, false, out var ue4ssLoadoutItems);
        if (!found)
        {
            var ueAddon = loadout.InstallationInstance.GetGame() as IUnrealEngineGameAddon;
            if (ueAddon is null) yield break;
            yield return Diagnostics.CreateScriptingSystemRequiredButNotInstalled(
                ModCount: count,
                NexusModsUE4SSUri: ueAddon.UE4SSLink
            );

            yield break;
        }

        var isUE4SSEnabled = ue4ssLoadoutItems.Any(x => x.IsMarker && !x.AsLoadoutItemGroup().AsLoadoutItem().IsDisabled);
        if (isUE4SSEnabled) yield break;
        
        yield return Diagnostics.CreateScriptingSystemRequiredButDisabled(
            ModCount: count
        );
    }
}
