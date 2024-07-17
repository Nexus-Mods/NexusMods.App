using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Mods;
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
public abstract class APathBasedDependencyEmitter : ILoadoutDiagnosticEmitter
{
    /// <summary>
    /// The link to download the dependency.
    /// </summary>
    protected abstract NamedLink DownloadLink { get; }
    
    /// <summary>
    /// The name of the dependency.
    /// </summary>
    protected abstract string DependencyName { get; }
    
    /// <summary>
    /// All the paths returned by this property must exist for the dependency to be considered installed.
    /// </summary>
    protected internal abstract IEnumerable<GamePath> DependencyPaths { get; }
    
    /// <summary>
    /// The folders that contain the dependant mods, for example `bin/x64/plugins/cyber_engine_tweaks`.
    /// </summary>
    protected internal abstract GamePath[] DependantPaths { get; }
    
    /// <summary>
    /// The file extensions of the dependant files, for example `.lua` or `.dll`.
    /// </summary>
    protected internal abstract Extension[] DependantExtensions { get; }
    
    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        // Cache the properties
        var dependantPaths = DependantPaths;
        var dependantExtensions = DependantExtensions;
        
        var mods = loadout
            .GetEnabledMods()
            .Where(mod => mod.Files
                .Any(file => IsDependant(dependantPaths, dependantExtensions, file.To)))
            .ToList();

        if (mods.Count == 0)
            yield break;
        
        var (enabled, disabled) = FindDependency(loadout);

        if (enabled.HasValue)
            yield break;
        
        foreach (var mod in mods)
        {
            if (disabled.HasValue)
                yield return Diagnostics.CreateDisabledModDependency(mod.ToReference(loadout), disabled.Value.ToReference(loadout));
            else 
                yield return Diagnostics.CreateMissingModWithKnownNexusUri(mod.ToReference(loadout), DependencyName, DownloadLink);
        }
    }

    private (Optional<Mod.ReadOnly> Enabled, Optional<Mod.ReadOnly> Disabled) FindDependency(Loadout.ReadOnly loadout)
    {
        var enabled = Optional<Mod.ReadOnly>.None;
        var disabled = Optional<Mod.ReadOnly>.None;
        
        foreach (var mod in loadout.Mods)
        {
            if (DependencyPaths.All(dependencyPath => mod.Files.Any(file => file.To == dependencyPath)))
            {
                if (mod.Enabled)
                {
                    // If it's enabled, we don't need to check for disabled mods or anything else.
                    enabled = mod;
                    break;
                }
                else
                {
                    disabled = mod;
                }
            }
        }
        
        return (enabled, disabled);
    }

    private bool IsDependant(GamePath[] dependantPaths, Extension[] dependantExtensions, GamePath path) =>
        dependantExtensions.Contains(path.Extension) && dependantPaths.Any(path.InFolder);
}
