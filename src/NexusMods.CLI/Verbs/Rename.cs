using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ModLists;

namespace NexusMods.CLI.Verbs;

public class Rename
{
    private readonly ModListManager _manager;

    public Rename(ModListManager manager)
    {
        _manager = manager;
    }
    public static VerbDefinition Definition = new VerbDefinition("rename",
        "Rename a loadout id to a specific registry name", new OptionDefinition[]
        {
            new OptionDefinition<ModList>("l", "loadOut", "Loadout to assign a name"),
            new OptionDefinition<string>("n", "name", "Name to assign the loadout")
        });
    
    public async Task Run(ModList loadOut, string name)
    {
        _manager.Alter(loadOut.ModListId, _ => loadOut, $"Renamed {loadOut.Id} to {name}");
    }
}