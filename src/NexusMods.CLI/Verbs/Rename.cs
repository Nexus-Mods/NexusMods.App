using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.CLI.Verbs;

public class Rename
{
    private readonly LoadoutManager _manager;

    public Rename(LoadoutManager manager)
    {
        _manager = manager;
    }
    public static VerbDefinition Definition = new VerbDefinition("rename",
        "Rename a loadout id to a specific registry name", new OptionDefinition[]
        {
            new OptionDefinition<Loadout>("l", "loadout", "Loadout to assign a name"),
            new OptionDefinition<string>("n", "name", "Name to assign the loadout")
        });
    
    public Task Run(Loadout loadout, string name)
    {
        _manager.Alter(loadout.LoadoutId, _ => loadout, $"Renamed {loadout.Id} to {name}");
        return Task.CompletedTask;
    }
}