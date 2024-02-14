using Bannerlord.LauncherManager;
using Bannerlord.LauncherManager.Localization;
using Bannerlord.ModuleManager;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
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
        foreach (var moduleViewModel in viewModels)
        {
            foreach (var diagnostic in ModuleUtilities.ValidateModule(modules, moduleViewModel.ModuleInfoExtended, ctx.GetIsSelected, ctx.GetIsValid).Select(x => Render(loadout, moduleViewModel.Mod, x)))
            {
                yield return diagnostic;
            }
        }
    }

    private static Diagnostic Render(Loadout loadout, Mod mod, ModuleIssue issue)
    {
        static string Version(ApplicationVersionRange version) => version == ApplicationVersionRange.Empty
            ? version.ToString()
            : version.Min == version.Max
                ? version.Min.ToString()
                : "";

        // We reuse the translation for now
        var (level, message) = issue.Type switch
        {
            ModuleIssueType.Missing => (DiagnosticSeverity.Critical, new BUTRTextObject("{=J3Uh6MV4}Missing '{ID}' {VERSION} in modules list")
                .SetTextVariable("ID", issue.SourceId)
                .SetTextVariable("VERSION", issue.SourceVersion.Min.ToString())),

            ModuleIssueType.MissingDependencies => (DiagnosticSeverity.Critical, new BUTRTextObject("{=3eQSr6wt}Missing '{ID}' {VERSION}")
                .SetTextVariable("ID", issue.SourceId)
                .SetTextVariable("VERSION", Version(issue.SourceVersion))),
            ModuleIssueType.DependencyMissingDependencies => (DiagnosticSeverity.Critical, new BUTRTextObject("{=U858vdQX}'{ID}' is missing it's dependencies!")
                .SetTextVariable("ID", issue.SourceId)),

            ModuleIssueType.DependencyValidationError => (DiagnosticSeverity.Critical, new BUTRTextObject("{=1LS8Z5DU}'{ID}' has unresolved issues!")
                .SetTextVariable("ID", issue.SourceId)),

            ModuleIssueType.VersionMismatchLessThanOrEqual => (DiagnosticSeverity.Warning, new BUTRTextObject("{=Vjz9HQ41}'{ID}' wrong version <= {VERSION}")
                .SetTextVariable("ID", issue.SourceId)
                .SetTextVariable("VERSION", Version(issue.SourceVersion))),
            ModuleIssueType.VersionMismatchLessThan => (DiagnosticSeverity.Warning, new BUTRTextObject("{=ZvnlL7VE}'{ID}' wrong version < [{VERSION}]")
                .SetTextVariable("ID", issue.SourceId)
                .SetTextVariable("VERSION", Version(issue.SourceVersion))),
            ModuleIssueType.VersionMismatchGreaterThan => (DiagnosticSeverity.Warning, new BUTRTextObject("{=EfNuH2bG}'{ID}' wrong version > [{VERSION}]")
                .SetTextVariable("ID", issue.SourceId)
                .SetTextVariable("VERSION", Version(issue.SourceVersion))),

            ModuleIssueType.Incompatible => (DiagnosticSeverity.Warning, new BUTRTextObject("{=zXDidmpQ}'{ID}' is incompatible with this module")
                .SetTextVariable("ID", issue.SourceId)),

            ModuleIssueType.DependencyConflictDependentAndIncompatible => (DiagnosticSeverity.Critical, new BUTRTextObject("{=4KFwqKgG}Module '{ID}' is both depended upon and marked as incompatible")
                .SetTextVariable("ID", issue.SourceId)),
            ModuleIssueType.DependencyConflictDependentLoadBeforeAndAfter => (DiagnosticSeverity.Critical, new BUTRTextObject("{=9DRB6yXv}Module '{ID}' is both depended upon as LoadBefore and LoadAfter")
                .SetTextVariable("ID", issue.SourceId)),
            ModuleIssueType.DependencyConflictCircular => (DiagnosticSeverity.Critical, new BUTRTextObject("{=RC1V9BbP}Circular dependencies. '{TARGETID}' and '{SOURCEID}' depend on each other")
                .SetTextVariable("TARGETID", issue.Target.Id)
                .SetTextVariable("SOURCEID", issue.SourceId)),

            ModuleIssueType.DependencyNotLoadedBeforeThis => (DiagnosticSeverity.Warning, new BUTRTextObject("{=s3xbuejE}'{SOURCEID}' should be loaded before '{TARGETID}'")
                .SetTextVariable("TARGETID", issue.Target.Id)
                .SetTextVariable("SOURCEID", issue.SourceId)),

            ModuleIssueType.DependencyNotLoadedAfterThis => (DiagnosticSeverity.Warning, new BUTRTextObject("{=2ALJB7z2}'{SOURCEID}' should be loaded after '{TARGETID}'")
                .SetTextVariable("ID", issue.SourceId)),

            _ => throw new ArgumentOutOfRangeException(nameof(issue))
        };

        return new Diagnostic
        {
            Id = new DiagnosticId(Source, (ushort) issue.Type),
            Message = DiagnosticMessage.From(message.ToString()),
            Severity = level,
            DataReferences = new IDataReference[]
            {
                loadout.ToReference(),
                mod.ToReference(loadout)
            }
        };
    }
}
