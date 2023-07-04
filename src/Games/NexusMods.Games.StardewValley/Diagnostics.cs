using NexusMods.DataModel.Diagnostics;
using NexusMods.DataModel.Diagnostics.References;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.Games.StardewValley;

internal static class Diagnostics
{
    internal const string Source = "NexusMods.Games.StardewValley";

    internal static Diagnostic MissingRequiredDependency(string modName, string missingDependency, LoadoutId loadoutId, ModId modId)
    {
        return new Diagnostic
        {
            Id = new DiagnosticId(Source, 1),
            Message = DiagnosticMessage.From($"Mod '{modName}' is missing required dependency '{missingDependency}'"),
            Severity = DiagnosticSeverity.Warning,
            DataReferences = new IDataReference[]
            {
                new LoadoutReference
                {
                    DataId = loadoutId
                },
                new ModReference
                {
                    DataId = modId
                }
            }
        };
    }
}
