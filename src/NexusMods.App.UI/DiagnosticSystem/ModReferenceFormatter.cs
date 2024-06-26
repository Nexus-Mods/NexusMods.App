using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.DiagnosticSystem;

internal sealed class ModReferenceFormatter(IConnection conn) : IValueFormatter<ModReference>
{

    public void Format(IDiagnosticWriter writer, ref DiagnosticWriterState state, ModReference value)
    {
        // TODO: custom markdown control
        var mod = Mod.Load(conn.Db, value.DataId);
        writer.Write(ref state, $"'{mod.Name}'");
    }
}
