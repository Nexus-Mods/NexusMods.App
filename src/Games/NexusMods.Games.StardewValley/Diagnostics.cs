using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;

namespace NexusMods.Games.StardewValley;

internal static class Diagnostics
{
    internal const string Source = "NexusMods.Games.StardewValley";

    internal static Diagnostic MissingRequiredDependency(Loadout loadout, Mod mod, string missingDependency)
    {
        return new Diagnostic
        {
            Id = new DiagnosticId(Source, 1),
            Severity = DiagnosticSeverity.Warning,
            Message = DiagnosticMessage.From($"Mod '{mod.Name}' is missing required dependency '{missingDependency}'"),
            DataReferences = new Dictionary<DataReferenceDescription, IDataReference>
            {
                { DataReferenceDescription.Loadout, loadout.ToReference() },
                { DataReferenceDescription.Mod, mod.ToReference(loadout) },
            },
        };
    }
}
