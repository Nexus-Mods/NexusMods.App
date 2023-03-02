using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts.Markers;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
public class ListMods : AVerb<LoadoutMarker>
{
    private readonly IRenderer _renderer;
    public ListMods(Configurator configurator) => _renderer = configurator.Renderer;

    public static VerbDefinition Definition => new("list-mods",
        "List all the mods in a given managed game",
        new OptionDefinition[]
        {
            new OptionDefinition<LoadoutMarker>("l", "loadout", "The managed game to access")
        });

    public async Task<int> Run(LoadoutMarker loadout, CancellationToken token)
    {
        var rows = new List<object[]>();
        foreach (var mod in loadout.Value.Mods.Values)
            rows.Add(new object[] { mod.Name, mod.Files.Count });

        await _renderer.Render(new Table(new[] { "Name", "File Count" }, rows));
        return 0;
    }
}
