using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
public class FlattenList : AVerb<LoadoutMarker>
{
    private readonly IRenderer _renderer;
    private readonly LoadoutSynchronizer _loadoutSyncronizer;

    public FlattenList(Configurator configurator, LoadoutSynchronizer loadoutSynchronizer)
    {
        _renderer = configurator.Renderer;
        _loadoutSyncronizer = loadoutSynchronizer;
    }

    public static VerbDefinition Definition => new("flatten-list",
        "Flatten a loadout into the projected filesystem", new OptionDefinition[]
        {
            new OptionDefinition<LoadoutMarker>("l", "loadout", "loadout to target")
        });

    public async Task<int> Run(LoadoutMarker loadout, CancellationToken token)
    {
        var rows = new List<object[]>();
        foreach (var (path, mod) in (await _loadoutSyncronizer.FlattenLoadout(loadout.Value)).Files)
            rows.Add(new object[] { mod.Mod.Name, path });

        await _renderer.Render(new Table(new[] { "Mod", "To" }, rows));
        return 0;
    }
}
