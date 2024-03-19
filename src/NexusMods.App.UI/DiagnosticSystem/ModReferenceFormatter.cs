using System.Text;
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

    public void Format(IDiagnosticWriter writer, StringBuilder stringBuilder, ModReference value)
    {
        // TODO: custom markdown control
        var mod = _loadoutRegistry.Get(value.DataId);
        writer.Write(stringBuilder, mod?.Name ?? "MISSING MOD");
    }
}
