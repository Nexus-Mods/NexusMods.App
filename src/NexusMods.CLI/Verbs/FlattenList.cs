using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts.Markers;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
public class FlattenList : AVerb<LoadoutMarker>
{
    private readonly IRenderer _renderer;
    public FlattenList(Configurator configurator) => _renderer = configurator.Renderer;

    public static VerbDefinition Definition => new("flatten-list",
        "Flatten a loadout into the projected filesystem", new OptionDefinition[]
        {
            new OptionDefinition<LoadoutMarker>("l", "loadout", "loadout to target")
        });
    
    public async Task<int> Run(LoadoutMarker loadout, CancellationToken token)
    {
        var rows = new List<object[]>();
        foreach (var (file, mod) in loadout.FlattenList())
            rows.Add(new object[]{mod.Name, file.To});

        await _renderer.Render(new Table(new[] { "Mod", "To"}, rows));
        return 0;
    }
}