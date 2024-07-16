using System.Runtime.CompilerServices;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

public class Red4ExtMissingDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    public static readonly DiagnosticId Id = new(Diagnostics.Source, 1);
    
    private static readonly NamedLink Red4ExtDownloadLink = new("Nexus Mods", NexusModsUrlBuilder.CreateDiagnosticUri(Cyberpunk2077Game.StaticDomain.Value, "2380"));
    
    private static readonly Extension DllExtension = new(".dll");
    
    /// <summary>
    /// The path where Red4Ext plugins are installed.
    /// </summary>
    private static readonly GamePath Red4ExtPluginPath = new(LocationId.Game, "red4ext/plugins");
    
    /// <summary>
    /// The path where Red4Ext itself is installed.
    /// </summary>
    private static readonly GamePath Red4ExtPath = new(LocationId.Game, "red4ext/RED4ext.dll");
    
    /// <summary>
    /// The DLL hook file that loads Red4Ext.
    /// </summary>
    private static readonly GamePath Red4ExtLoaderPath = new(LocationId.Game, "bin/x64/winmm.dll");
    
    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var mods = loadout
            .GetEnabledMods()
            .Where(mod => mod.Files
                .Any(file => IsRed4ExtPlugin(file.To)))
            .ToList();

        if (mods.Count > 0)
        {
            if (Red4ExtExistsAndIsEnabled(loadout))
                yield break;
        }
        
        foreach (var mod in mods)
        {
            yield return Diagnostics.CreateMissingModWithKnownNexusUri(mod, "Red4Ext", Red4ExtDownloadLink);
        }
    }

    private bool Red4ExtExistsAndIsEnabled(Loadout.ReadOnly loadout)
    {
        var loaderFound = loadout.GetEnabledMods().Any(mod => mod.Files.Any(file => file.To == Red4ExtLoaderPath));
        var red4ExtFound = loadout.GetEnabledMods().Any(mod => mod.Files.Any(file => file.To == Red4ExtPath));
        
        return loaderFound && red4ExtFound;
    }

    public bool IsRed4ExtPlugin(GamePath path) => 
        path.Extension == DllExtension && 
        path.InFolder(Red4ExtPluginPath);
}
