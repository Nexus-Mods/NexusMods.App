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
        var loadout = Loadout.Load(conn.Db, value.DataId);
        if (loadout.IsValid())
        {
            writer.Write(ref state, loadout.Name);
        }
        else
        {
            writer.Write(ref state, $"Invalid Loadout entity: {value.DataId}");
        }
    }
}
