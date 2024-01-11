using Bannerlord.LauncherManager;
using Bannerlord.LauncherManager.Utils;
using Bannerlord.ModuleManager;
using NexusMods.DataModel.Diagnostics;
using NexusMods.DataModel.Diagnostics.Emitters;
using NexusMods.DataModel.Diagnostics.References;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;
using NexusMods.Games.MountAndBlade2Bannerlord.Utils;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Emitters;

public class BuiltInEmitter : ILoadoutDiagnosticEmitter
{
    internal const string Source = "NexusMods.Games.MountAndBlade2Bannerlord";

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout loadout)
    {
        await Task.Yield();

        var viewModels = (await loadout.GetSortedViewModelsAsync()).ToList();
        var lookup = viewModels.ToDictionary(x => x.ModuleInfoExtended.Id, x => x);
        var modules = lookup.Values.Select(x => x.ModuleInfoExtended).Concat(FeatureIds.LauncherFeatures.Select(x => new ModuleInfoExtended { Id = x })).ToList();

        var ctx = new ModuleContext(lookup);
        foreach (var vm in viewModels)
        {
            foreach (var diagnostic in ModuleUtilities.ValidateModule(modules, vm.ModuleInfoExtended, ctx.GetIsSelected, ctx.GetIsValid).Select(x => Render(loadout, vm.Mod, x)))
            {
                yield return diagnostic;
            }
        }
    }

    private static Diagnostic Render(Loadout loadout, Mod mod, ModuleIssue issue)
    {
        var level = issue.Type switch
        {
            ModuleIssueType.Missing => DiagnosticSeverity.Critical,

            ModuleIssueType.MissingDependencies => DiagnosticSeverity.Critical,
            ModuleIssueType.DependencyMissingDependencies => DiagnosticSeverity.Critical,

            ModuleIssueType.DependencyValidationError => DiagnosticSeverity.Critical,

            ModuleIssueType.VersionMismatchLessThanOrEqual => DiagnosticSeverity.Warning,
            ModuleIssueType.VersionMismatchLessThan => DiagnosticSeverity.Warning,
            ModuleIssueType.VersionMismatchGreaterThan => DiagnosticSeverity.Warning,

            ModuleIssueType.Incompatible => DiagnosticSeverity.Warning,

            ModuleIssueType.DependencyConflictDependentAndIncompatible => DiagnosticSeverity.Critical,
            ModuleIssueType.DependencyConflictDependentLoadBeforeAndAfter => DiagnosticSeverity.Critical,
            ModuleIssueType.DependencyConflictCircular => DiagnosticSeverity.Critical,

            ModuleIssueType.DependencyNotLoadedBeforeThis => DiagnosticSeverity.Warning,

            ModuleIssueType.DependencyNotLoadedAfterThis => DiagnosticSeverity.Warning,

            _ => throw new ArgumentOutOfRangeException(nameof(issue))
        };

        return new Diagnostic
        {
            Id = new DiagnosticId(Source, (ushort) issue.Type),
            Message = DiagnosticMessage.From(ModuleIssueRenderer.Render(issue)), // We reuse the translation for now
            Severity = level,
            DataReferences = new IDataReference[]
            {
                loadout.ToReference(),
                mod.ToReference(loadout)
            }
        };
    }
}
