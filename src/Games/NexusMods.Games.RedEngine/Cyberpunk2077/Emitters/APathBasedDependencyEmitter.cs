using System.Runtime.CompilerServices;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

/// <summary>
/// This is an abstract class that represents an emitter for known dependencies that are based on paths. For example
/// if you need to look for files of a given type and path, that require a mod loader which also has a specific path
/// and file extension, this is what you need.
///
/// For example: Cyberpunk2077 has Cyber Engine Tweaks which installs a mod loader at the path `bin/x64/version.dll`
/// and the mods are installed at `bin/x64/plugins/cyber_engine_tweaks/{ModName}/` with the file extension `.lua`.
/// Armed just with these paths and this class, you can create a diagnostic that checks if the mod loader is installed
/// and enabled.
/// 
/// </summary>
public abstract class APathBasedDependencyEmitter : IDiagnosticEmitter
{
    /// <summary>
    /// The link to download the dependency.
    /// </summary>
    public abstract NamedLink DownloadLink { get; }
    
    /// <summary>
    /// The name of the dependency.
    /// </summary>
    public abstract string DependencyName { get; }
    
    /// <summary>
    /// All the paths returned by this property must exist for the dependency to be considered installed.
    /// </summary>
    public abstract IEnumerable<GamePath> DependencyPaths { get; }
    
    /// <summary>
    /// The folder that contains the dependant mods for example `bin/x64/plugins/cyber_engine_tweaks`.
    /// </summary>
    public abstract GamePath DependantPath { get; }
    
    /// <summary>
    /// The file extension of the dependant mods, for example `.lua` or `.dll`.
    /// </summary>
    public abstract Extension DependantExtension { get; }
    
    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var mods = loadout
            .GetEnabledMods()
            .Where(mod => mod.Files
                .Any(file => IsDependant(file.To)))
            .ToList();

        if (mods.Count > 0 || DependencyIsInstalled(loadout))
            yield break;
        
        foreach (var mod in mods)
        {
            yield return Diagnostics.CreateRed4ExtMissing(mod, CETDownloadLink);
        }
        
        
    }

    private bool DependencyIsInstalled(Loadout.ReadOnly loadout)
    {
        // This is absolute shit for performance, but we need a solid query/logic engine before I want to optimize it
        return DependencyPaths.All(dependencyPath => loadout.GetEnabledMods().Any(mod => mod.Files.Any(file => file.To == dependencyPath)));
    }

    public bool IsDependant(GamePath path) => 
        path.Extension == DependantExtension && 
        path.InFolder(DependantPath);
}
