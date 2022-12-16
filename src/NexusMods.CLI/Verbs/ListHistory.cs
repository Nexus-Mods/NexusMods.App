using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts.Markers;

namespace NexusMods.CLI.Verbs;

public class ListHistory
{
    private readonly IRenderer _renderer;

    public ListHistory(Configurator configurator)
    {
        _renderer = configurator.Renderer;
    }
    
    public static VerbDefinition Definition => new("list-history", "Lists the history of a loadout",
        new OptionDefinition[]
        {
            new OptionDefinition<LoadoutMarker>("m", "loadout", "Loadout to load")
        });

    public async Task Run(LoadoutMarker loadout)
    {
        var rows = loadout.History()
            .Select(list => new object[] { list.LastModified, list.ChangeMessage, list.Mods.Count, list.Id })
            .ToList();
        
        await _renderer.Render(new Table(new string[] { "Date", "Change Message", "Mod Count", "Id" }, rows));
    }
}