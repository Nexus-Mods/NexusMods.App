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
        
        var groups = loadout.Items.GetEnabledLoadoutFiles()
            .Where(file => IsDependant(dependantPaths, dependantExtensions, file.AsLoadoutItemWithTargetPath().TargetPath))
            .Select(file => file.AsLoadoutItemWithTargetPath().AsLoadoutItem().Parent)
            .Distinct()
            .ToList();

        if (groups.Count == 0)
            yield break;
        
        var (enabled, disabled) = FindDependency(loadout);

        if (enabled.HasValue)
            yield break;
        
        foreach (var group in groups)
        {
            if (disabled.HasValue)
                yield return Diagnostics.CreateDisabledGroupDependency(group.ToReference(loadout), disabled.Value.ToReference(loadout));
            else 
                yield return Diagnostics.CreateMissingModWithKnownNexusUri(group.ToReference(loadout), DependencyName, DownloadLink);
        }
    }

    private (Optional<LoadoutItemGroup.ReadOnly> Enabled, Optional<LoadoutItemGroup.ReadOnly> Disabled) FindDependency(Loadout.ReadOnly loadout)
    {
        var enabled = Optional<LoadoutItemGroup.ReadOnly>.None;
        var disabled = Optional<LoadoutItemGroup.ReadOnly>.None;
        
        foreach (var group in loadout.Items.OfTypeLoadoutItemGroup())
        {
            if (DependencyPaths.All(dependencyPath => group.Children.OfTypeLoadoutItemWithTargetPath().Any(file => file.TargetPath == dependencyPath)))
            {
                if (group.AsLoadoutItem().IsEnabled())
                {
                    // If it's enabled, we don't need to check for disabled mods or anything else.
                    enabled = group;
                    break;
                }
                else
                {
                    disabled = group;
                }
            }
        }
        
        return (enabled, disabled);
    }

    private bool IsDependant(GamePath[] dependantPaths, Extension[] dependantExtensions, GamePath path) =>
        dependantExtensions.Contains(path.Extension) && dependantPaths.Any(path.InFolder);
}
