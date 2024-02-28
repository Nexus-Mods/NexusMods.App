using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.DataModel.Tests.Diagnostics;

public class DummyLoadoutDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    internal static DiagnosticMessage CreateMessage(Loadout loadout)
    {
        return DiagnosticMessage.From($"LoadoutId={loadout.LoadoutId}, DataStoreId={loadout.DataStoreId.SpanHex}");
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout loadout)
    {
        yield return new Diagnostic
        {
            Id = new DiagnosticId("NexusMods.DataModel.Tests.Diagnostics.DummyLoadoutDiagnosticEmitter", 1),
            Severity = DiagnosticSeverity.Critical,
            Summary = CreateMessage(loadout),
            Details = DiagnosticMessage.DefaultValue,
            DataReferences = new Dictionary<DataReferenceDescription, IDataReference>()
            {
                { DataReferenceDescription.Loadout, loadout.ToReference() }
            }
        };
    }
}
