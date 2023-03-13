using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// TODO: We don't have an import loadout option.

// ReSharper disable once ClassNeverInstantiated.Global
public class ExportLoadout : AVerb<LoadoutMarker, AbsolutePath>
{
    public static VerbDefinition Definition => new("export-loadout", "Export a loadout to a file",
        new OptionDefinition[]
        {
            new OptionDefinition<LoadoutMarker>("l", "loadout", "The loadout to export"),
            new OptionDefinition<AbsolutePath>("o", "output", "The file to export to")
        });

    public async Task<int> Run(LoadoutMarker loadout, AbsolutePath output, CancellationToken token)
    {
        await loadout.ExportToAsync(output, token);
        return 0;
    }
}
