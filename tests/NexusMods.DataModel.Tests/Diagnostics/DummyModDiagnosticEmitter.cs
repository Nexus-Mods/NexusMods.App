using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.DataModel.Tests.Diagnostics;

public class DummyModDiagnosticEmitter : IModDiagnosticEmitter
{
    internal static DiagnosticMessage CreateMessage(Loadout loadout, Mod mod)
    {
        return DiagnosticMessage.From($"Loadout={loadout.LoadoutId}, Mod={mod.Id}");
    }

    public IEnumerable<Diagnostic> Diagnose(Loadout loadout, Mod mod)
    {
        yield return new Diagnostic
        {
            Id = new DiagnosticId("NexusMods.DataModels.Tests.Diagnostics.DummyModDiagnosticEmitter", 1),
            Severity = DiagnosticSeverity.Critical,
            Message = CreateMessage(loadout, mod),
            DataReferences = new IDataReference[]
            {
                loadout.ToReference(),
                mod.ToReference(loadout)
            }
        };
    }
}
