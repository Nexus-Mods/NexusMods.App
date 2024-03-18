using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.App.UI.DiagnosticSystem;

internal sealed class LoadoutReferenceFormatter : IValueFormatter<LoadoutReference>
{
    private readonly ILoadoutRegistry _loadoutRegistry;

    public LoadoutReferenceFormatter(ILoadoutRegistry loadoutRegistry)
    {
        _loadoutRegistry = loadoutRegistry;
    }

    public void Format(LoadoutReference value, IDiagnosticWriter writer)
    {
        // TODO: custom markdown control
        var loadout = _loadoutRegistry.Get(value.DataId);
        writer.Write(loadout?.Name ?? "MISSING LOADOUT");
    }
}
