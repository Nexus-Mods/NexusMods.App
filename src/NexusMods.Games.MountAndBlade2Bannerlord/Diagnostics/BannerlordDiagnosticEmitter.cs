using Bannerlord.ModuleManager;
using Bannerlord.ModuleManager.Models.Issues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
namespace NexusMods.Games.MountAndBlade2Bannerlord.Diagnostics;

/// <summary>
/// This is a diagnostic emitter powered by the <see cref="LauncherManager"/> framework.
/// Errors are emitted by the <see cref="LauncherManager"/>, and we filter and parse them into the UI.
///
/// Only caveat, is we get the current state from loadout, rather than from real disk.
/// </summary>
internal partial class BannerlordDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private static readonly NamedLink BlseLink = new("Bannerlord Software Extender", NexusModsUrlBuilder.GetModUri(Bannerlord.DomainStatic, ModId.From(1), campaign: NexusModsUrlBuilder.CampaignDiagnostics));
    private static readonly NamedLink HarmonyLink = new("Harmony",NexusModsUrlBuilder.GetModUri(Bannerlord.DomainStatic, ModId.From(2006), campaign: NexusModsUrlBuilder.CampaignDiagnostics));
    private readonly IResourceLoader<BannerlordModuleLoadoutItem.ReadOnly, ModuleInfoExtended> _manifestPipeline;
    private readonly ILogger _logger;

    public BannerlordDiagnosticEmitter(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<BannerlordDiagnosticEmitter>>();
        _manifestPipeline = Pipelines.GetManifestPipeline(serviceProvider);
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        var modulesAndMods = await Helpers.GetAllManifestsAsync(_logger, loadout, _manifestPipeline, cancellationToken).ToArrayAsync(cancellationToken);
        var modulesOnly = modulesAndMods.Select(x => x.Item2).ToArray();
        var isEnabledDict = new Dictionary<ModuleInfoExtended, bool>(modulesOnly.Length);

        // Populate the dict declaring if item is enabled.
        foreach (var module in modulesAndMods)
        {
            var mod = module.Item1;
            var loadoutItem = mod.AsLoadoutItemGroup().AsLoadoutItem();
            
            // Note(sewer): We create a LoadoutItemGroup for each module, which is a child of the one
            //              used for the archive. Since in theory the item can be disabled at any level
            //              in the tree, we need to check if the parent is disabled.
            var isEnabled = loadoutItem.IsEnabled();
            isEnabledDict[module.Item2] = isEnabled;
        }
        
        // TODO: HACK. Pretend base game modules are installed before we can properly ingest them.
        foreach (var module in Hack.GetDummyBaseGameModules())
            isEnabledDict[module] = true;
        modulesOnly = modulesOnly.Concat(Hack.GetDummyBaseGameModules()).ToArray();
        // TODO: HACK. Pretend base game modules are installed before we can properly ingest them.

        // Emit diagnostics
        var isBlseInstalled = loadout.IsBLSEInstalled();
        if (isBlseInstalled && !IsHarmonyAvailable(modulesOnly, isEnabledDict))
            yield return Diagnostics.CreateMissingHarmony(HarmonyLink);

        foreach (var moduleAndMod in isEnabledDict)
        {
            var moduleInfo = moduleAndMod.Key;
            if (!moduleAndMod.Value)
                continue;

            // Note(sewer): All modules are valid by definition
            //              All modules are selected by definition.
            foreach (var diagnostic in ModuleUtilities.ValidateModuleEx(modulesOnly, moduleInfo, module => isEnabledDict.ContainsKey(module), _ => true, false).Select(x => CreateDiagnostic(x, isBlseInstalled)))
            {
                if (diagnostic != null)
                    yield return diagnostic;
            }
        }
    }

    private Diagnostic? CreateDiagnostic(ModuleIssueV2 issue, bool isBlseInstalled)
    {
        return issue switch
        {
            // API Error
            ModuleMissingIssue missingIssue => null,

            // Dependency Missing Its Own Dependencies
            // Note(sewer): We emit this from the dependency mod itself.
            ModuleDependencyMissingDependenciesIssue dependencyMissingDeps => null,

            // Dependency Validation Issue
            // Note(sewer): We emit this from the dependency mod itself.
            ModuleDependencyValidationIssue dependencyValidation => null,

            // Missing BLSE Dependency
            ModuleMissingBLSEDependencyIssue missingUnversioned when !isBlseInstalled => Diagnostics.CreateMissingBLSE(
                ModId: missingUnversioned.Module.Id,
                ModName: missingUnversioned.Module.Name,
                DependencyId: missingUnversioned.Dependency.Id,
                BLSELink: BlseLink
            ),
            
            // Missing Unversioned Dependency
            ModuleMissingUnversionedDependencyIssue missingUnversioned => Diagnostics.CreateMissingDependency(
                ModId: missingUnversioned.Module.Id,
                ModName: missingUnversioned.Module.Name,
                DependencyId: missingUnversioned.Dependency.Id
            ),

            // Missing Dependency with Exact Version
            ModuleMissingExactVersionDependencyIssue missingExact => Diagnostics.CreateMissingDependencyWithVersion(
                ModId: missingExact.Module.Id,
                ModName: missingExact.Module.Name,
                DependencyId: missingExact.Dependency.Id,
                Version: missingExact.Dependency.Version.ToString()
            ),

            // Missing Dependency with Version Range
            ModuleMissingVersionRangeDependencyIssue missingRange => Diagnostics.CreateMissingVersionRange(
                ModId: missingRange.Module.Id,
                ModName: missingRange.Module.Name,
                DependencyId: missingRange.Dependency.Id,
                VersionRange: missingRange.Dependency.VersionRange.ToString()
            ),

            // Version Mismatch: Installed Version Too Low
            ModuleVersionTooLowIssue mismatchLow => Diagnostics.CreateModVersionTooLow(
                ModId: mismatchLow.Module.Id,
                ModName: mismatchLow.Module.Name,
                DependencyId: mismatchLow.Dependency.Id,
                DependencyName: mismatchLow.Dependency.Name,
                Version: mismatchLow.Version.ToString(),
                InstalledVersion: mismatchLow.Dependency.Version.ToString()
            ),

            // Version Mismatch: Installed Version Too Low (Range)
            ModuleVersionMismatchLessThanRangeIssue mismatchRangeLow => Diagnostics.CreateModVersionRangeTooLow(
                ModId: mismatchRangeLow.Module.Id,
                ModName: mismatchRangeLow.Module.Name,
                DependencyId: mismatchRangeLow.Dependency.Id,
                VersionRange: mismatchRangeLow.VersionRange.ToString(),
                InstalledVersion: mismatchRangeLow.Dependency.Version.ToString()
            ),

            // Version Mismatch: Installed Version Too High (Range)
            ModuleVersionMismatchGreaterThanRangeIssue mismatchRangeHigh => Diagnostics.CreateModVersionRangeTooHigh(
                ModId: mismatchRangeHigh.Module.Id,
                ModName: mismatchRangeHigh.Module.Name,
                DependencyId: mismatchRangeHigh.Dependency.Id,
                VersionRange: mismatchRangeHigh.VersionRange.ToString(),
                InstalledVersion: mismatchRangeHigh.Dependency.Version.ToString()
            ),

            // Incompatible Module
            ModuleIncompatibleIssue incompatibleIssue => Diagnostics.CreateModIncompatible(
                ModId: incompatibleIssue.Module.Id,
                ModName: incompatibleIssue.Module.Name,
                IncompatibleId: incompatibleIssue.IncompatibleModule.Id,
                IncompatibleName: incompatibleIssue.IncompatibleModule.Name
            ),

            // Conflicting Dependency: Dependent and Incompatible
            ModuleDependencyConflictDependentAndIncompatibleIssue conflictDepAndInc => Diagnostics.CreateBothRequiredAndIncompatible(
                ModId: conflictDepAndInc.Module.Id,
                ModName: conflictDepAndInc.Module.Name,
                ConflictingId: conflictDepAndInc.ConflictingModuleId
            ),

            // Conflicting Load Order: Load Before and After
            ModuleDependencyConflictLoadBeforeAndAfterIssue conflictLoad => Diagnostics.CreateLoadOrderConflict(
                ModId: conflictLoad.Module.Id,
                ModName: conflictLoad.Module.Name,
                ConflictingId: conflictLoad.ConflictingModule.Id
            ),

            // Circular Dependency
            ModuleDependencyConflictCircularIssue circularIssue => Diagnostics.CreateCircularDependency(
                ModId: circularIssue.Module.Id,
                ModName: circularIssue.Module.Name,
                CircularDependencyId: circularIssue.CircularDependency.Id,
                CircularDependencyName: circularIssue.CircularDependency.Name
            ),

            // Load Order Not Met: Must Load Before
            ModuleDependencyNotLoadedBeforeIssue notLoadedBefore => Diagnostics.CreateModMustLoadBefore(
                ModId: notLoadedBefore.Module.Id,
                ModName: notLoadedBefore.Module.Name,
                DependencyId: notLoadedBefore.Dependency.Id
            ),

            // Load Order Not Met: Must Load After
            ModuleDependencyNotLoadedAfterIssue notLoadedAfter => Diagnostics.CreateModMustLoadAfter(
                ModId: notLoadedAfter.Module.Id,
                ModName: notLoadedAfter.Module.Name,
                DependencyId: notLoadedAfter.Dependency.Id
            ),

            // Configuration Error: Missing ID
            ModuleMissingIdIssue missingId => Diagnostics.CreateModBadConfigMissingId(
                ModName: missingId.Module.Name,
                ModNameNoSpaces: missingId.Module.Name.Replace(" ", "")
            ),

            // Configuration Error: Missing Name
            ModuleMissingNameIssue missingName => Diagnostics.CreateModBadConfigMissingName(
                ModId: missingName.Module.Id
            ),

            // Configuration Error: Null Dependency
            ModuleDependencyNullIssue nullDependency => Diagnostics.CreateModBadConfigNullDependency(
                ModId: nullDependency.Module.Id,
                ModName: nullDependency.Module.Name
            ),

            // Configuration Error: Dependency Missing ID
            ModuleDependencyMissingIdIssue missingDependencyId => Diagnostics.CreateModBadConfigDependencyMissingId(
                ModId: missingDependencyId.Module.Id,
                ModName: missingDependencyId.Module.Name
            ),

            // Base classes, ignore.
            ModuleVersionMismatchRangeIssue moduleVersionMismatchRangeIssue => null,
            ModuleVersionMismatchSpecificIssue moduleVersionMismatchSpecificIssue => null,
            ModuleVersionMismatchIssue moduleVersionMismatchIssue => null,
            
            // A new variant that's unknown to our code.
            _ => LogAndReturnUnknownDiagnostic(issue), 
        };
    }

    private Diagnostic? LogAndReturnUnknownDiagnostic(ModuleIssueV2 issue)
    {
        _logger.LogError("Unknown issue. This indicates an update in `Bannerlord.ModuleManager`" +
                         "which is not handled on our end in a switch statement.\n" +
                         "Issue text is below: {Issue}", issue);
        return null;
    }

    private bool IsHarmonyAvailable(ModuleInfoExtended[] modulesOnly, Dictionary<ModuleInfoExtended, bool> isEnabledDict)
    {
        var harmonyModule = modulesOnly.FirstOrDefault(x => x.Id == "Bannerlord.Harmony");
        return harmonyModule != default(ModuleInfoExtended?) && isEnabledDict[harmonyModule];
    }
}
