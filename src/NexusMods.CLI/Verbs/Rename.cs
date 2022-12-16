using NexusMods.DataModel.Loadouts;
#pragma warning disable CS1998

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
            new OptionDefinition<Loadout>("l", "loadOut", "Loadout to assign a name"),
            new OptionDefinition<string>("n", "name", "Name to assign the loadout")
        });
    
    public async Task Run(Loadout loadOut, string name)
    {
        _manager.Alter(loadOut.LoadoutId, _ => loadOut, $"Renamed {loadOut.Id} to {name}");
    }
}