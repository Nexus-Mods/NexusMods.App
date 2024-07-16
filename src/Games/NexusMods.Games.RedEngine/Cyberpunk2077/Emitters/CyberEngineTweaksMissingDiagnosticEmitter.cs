using System.Runtime.CompilerServices;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

public class CyberEngineTweaksMissingDiagnosticEmitter
{
    public static readonly DiagnosticId Id = new(Diagnostics.Source, 2);
    
    private static readonly NamedLink CETDownloadLink = new("Cyber Engine Tweaks", new("https://www.nexusmods.com/cyberpunk2077/mods/107"));
    
    private static readonly Extension LuaExtension = new(".lua");
    
    /// <summary>
    /// The path where CET plugins are installed.
    /// </summary>
    private static readonly GamePath CETPluginPath = new(LocationId.Game, "bin/x64/plugins/cyber_engine_tweaks");
    
    /// <summary>
    /// The path where Red4Ext itself is installed.
    /// </summary>
    private static readonly GamePath CETExtensionPath = new(LocationId.Game, "bin/x64/version.dll");

    
    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var mods = loadout
            .GetEnabledMods()
            .Where(mod => mod.Files
                .Any(file => IsCETPlugin(file.To)))
            .ToList();

        if (mods.Count > 0)
        {
            if (CETInstalledAndEnabled(loadout))
                yield break;
        }
        /*
        foreach (var mod in mods)
        {
            yield return Diagnostics.CreateRed4ExtMissing(mod, CETDownloadLink);
        }
        */
        
    }

    private bool CETInstalledAndEnabled(Loadout.ReadOnly loadout)
    {
        var loaderFound = loadout.GetEnabledMods().Any(mod => mod.Files.Any(file => file.To == CETExtensionPath));
        return loaderFound;
    }

    public bool IsCETPlugin(GamePath path) => 
        path.Extension == LuaExtension && 
        path.InFolder(CETPluginPath);
}
