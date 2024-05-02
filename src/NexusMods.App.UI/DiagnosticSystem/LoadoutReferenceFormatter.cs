using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.DiagnosticSystem;

internal sealed class LoadoutReferenceFormatter(IConnection conn) : IValueFormatter<LoadoutReference>
{
    public void Format(IDiagnosticWriter writer, ref DiagnosticWriterState state, LoadoutReference value)
    {
        // TODO: custom markdown control
        var loadout = conn.Db.Get(value.DataId);
        writer.Write(ref state, loadout?.Name ?? "MISSING LOADOUT");
    }
}
