using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// TODO: We don't have an import loadout option. https://github.com/Nexus-Mods/NexusMods.App/issues/205

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Export a loadout to a file
/// </summary>
public class ExportLoadout : AVerb<LoadoutMarker, AbsolutePath>
{
    private readonly LoadoutManager _loadoutManager;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="loadoutManager"></param>
    public ExportLoadout(LoadoutManager loadoutManager)
    {
        _loadoutManager = loadoutManager;
    }

    /// <inheritdoc />
    public static VerbDefinition Definition => new("export-loadout", "Export a loadout to a file",
        new OptionDefinition[]
        {
            new OptionDefinition<LoadoutMarker>("l", "loadout", "The loadout to export"),
            new OptionDefinition<AbsolutePath>("o", "output", "The file to export to")
        });

    /// <summary>
    /// Export a loadout to a file
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="output"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> Run(LoadoutMarker loadout, AbsolutePath output, CancellationToken token)
    {
        // TODO: Fix this
        //await _loadoutManager.ExportToAsync(output, token);
        return Task.FromResult(0);
    }
}
