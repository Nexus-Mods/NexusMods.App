using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts.Markers;

namespace NexusMods.CLI.Verbs;

public class FlattenList
{
    private readonly IRenderer _renderer;
    public FlattenList(Configurator configurator)
    {
        _renderer = configurator.Renderer;
    }

    
    public static VerbDefinition Definition = new VerbDefinition("flatten-list",
        "Flatten a mod list into the projected filesystem", new[]
        {
            new OptionDefinition<LoadoutMarker>("m", "loadout", "Mod List to target")
        });
    
    public async Task Run(LoadoutMarker loadout, CancellationToken token)
    {
        var rows = new List<object[]>();
        foreach (var (file, mod) in loadout.FlattenList())
        {
            rows.Add(new object[]{mod.Name, file.To});
        }

        await _renderer.Render(new Table(new[] { "Mod", "To"}, rows));
    }
}