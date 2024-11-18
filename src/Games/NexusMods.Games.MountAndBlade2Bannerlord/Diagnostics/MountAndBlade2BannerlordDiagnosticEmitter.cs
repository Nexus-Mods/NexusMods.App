using Bannerlord.ModuleManager;
using Bannerlord.ModuleManager.Models.Issues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Resources;
using NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
namespace NexusMods.Games.MountAndBlade2Bannerlord.Diagnostics;

/// <summary>
/// This is a diagnostic emitter powered by the <see cref="LauncherManager"/> framework.
/// Errors are emitted by the <see cref="LauncherManager"/>, and we filter and parse them into the UI.
///
/// Only caveat, is we get the current state from loadout, rather than from real disk.
/// </summary>
internal partial class MountAndBlade2BannerlordDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private readonly IResourceLoader<BannerlordModuleLoadoutItem.ReadOnly, ModuleInfoExtended> _manifestPipeline;
    private readonly ILogger _logger;

    public MountAndBlade2BannerlordDiagnosticEmitter(IServiceProvider serviceProvider)
    {
        serviceProvider.GetRequiredService<LauncherManagerFactory>();
        _logger = serviceProvider.GetRequiredService<ILogger<MountAndBlade2BannerlordDiagnosticEmitter>>();
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
            var isEnabled = !mod.AsLoadoutItemGroup().AsLoadoutItem().IsDisabled;
            isEnabledDict[module.Item2] = isEnabled;
        }
        
        foreach (var moduleAndMod in modulesAndMods)
        {
            var (_, moduleInfo) = moduleAndMod;
            // Note(sewer): All modules are valid by definition
            //              All modules are selected by definition.
            foreach (var diagnostic in ModuleUtilities.ValidateModuleEx(modulesOnly, moduleInfo, module => isEnabledDict.ContainsKey(module), _ => true, false).Select(x => CreateDiagnostic(x)))
            {
                if (diagnostic != null)
                    yield return diagnostic;
            }
        }
    }

    private static Diagnostic? CreateDiagnostic(ModuleIssueV2 issue)
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
            _ => throw new ArgumentOutOfRangeException(nameof(issue), issue, null),
        };
    }
}
