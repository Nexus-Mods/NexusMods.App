using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;

namespace NexusMods.CLI.Verbs;

public class ListMods
{
    private readonly IRenderer _renderer;
    public ListMods(Configurator configurator)
    {
        _renderer = configurator.Renderer;
    }

    public static VerbDefinition Definition = new("list-mods",
        "List all the mods in a given managed game",
        new[]
        {
            new OptionDefinition<LoadoutMarker>("m", "managedGame", "The managed game to access")
        });


    public async Task Run(LoadoutMarker managedGame, CancellationToken token)
    {
        var rows = new List<object[]>();
        foreach (var mod in managedGame.Value.Mods)
        {
            rows.Add(new object[]{mod.Name, mod.Files.Count});
        }

        await _renderer.Render(new Table(new[] { "Name", "File Count"}, rows));
    }
}