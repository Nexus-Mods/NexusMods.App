using NexusMods.DataModel.ModLists;
using NexusMods.DataModel.ModLists.Markers;

namespace NexusMods.CLI.Verbs;

public class ListMods
{
    public static VerbDefinition Definition = new VerbDefinition("list-mods",
        "List all the mods in a given managed game",
        new[]
        {
            new OptionDefinition(typeof(ModListMarker), "m", "managedGame", "The managed game to access")
        });

    public async Task Run(ModListMarker managedGame, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}