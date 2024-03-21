using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.App.UI.DiagnosticSystem;

internal sealed class ModReferenceFormatter : IValueFormatter<ModReference>
{
    private readonly ILoadoutRegistry _loadoutRegistry;

    public ModReferenceFormatter(ILoadoutRegistry loadoutRegistry)
    {
        _loadoutRegistry = loadoutRegistry;
    }

    public void Format(IDiagnosticWriter writer, ref DiagnosticWriterState state, ModReference value)
    {
        // TODO: custom markdown control
        var mod = _loadoutRegistry.Get(value.DataId);
        writer.Write(ref state, mod?.Name ?? "MISSING MOD");
    }
}
