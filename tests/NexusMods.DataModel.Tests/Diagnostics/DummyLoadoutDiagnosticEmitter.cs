using NexusMods.DataModel.Diagnostics;
using NexusMods.DataModel.Diagnostics.Emitters;
using NexusMods.DataModel.Diagnostics.References;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Tests.Diagnostics;

public class DummyLoadoutDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    internal static DiagnosticMessage CreateMessage(Loadout loadout)
    {
        return DiagnosticMessage.From($"LoadoutId={loadout.LoadoutId}, DataStoreId={loadout.DataStoreId.SpanHex}");
    }

    public IEnumerable<Diagnostic> Diagnose(Loadout loadout)
    {
        yield return new Diagnostic
        {
            Id = new DiagnosticId("NexusMods.DataModel.Tests.Diagnostics.DummyLoadoutDiagnosticEmitter", 1),
            Message = CreateMessage(loadout),
            Severity = DiagnosticSeverity.Critical,
            DataReferences = new[]
            {
                loadout.ToReference()
            }
        };
    }
}
