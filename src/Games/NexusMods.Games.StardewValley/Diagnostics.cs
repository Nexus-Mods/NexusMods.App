using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Games.StardewValley;

internal static class Diagnostics
{
    internal const string Source = "NexusMods.Games.StardewValley";

    internal static Diagnostic MissingRequiredDependency(Loadout loadout, Mod mod, string missingDependency)
    {
        return new Diagnostic
        {
            Id = new DiagnosticId(Source, 1),
            Message = DiagnosticMessage.From($"Mod '{mod.Name}' is missing required dependency '{missingDependency}'"),
            Severity = DiagnosticSeverity.Warning,
            DataReferences = new IDataReference[]
            {
                loadout.ToReference(),
                mod.ToReference(loadout)
            }
        };
    }
}
