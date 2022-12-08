using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.ModLists.Markers;
using NexusMods.DataModel.ModLists.ModFiles;

namespace NexusMods.CLI.Verbs;

public class ListModContents
{
    private readonly IRenderer _renderer;
    public ListModContents(Configurator configurator)
    {
        _renderer = configurator.Renderer;
    }
    
    public static VerbDefinition Definition = new("list-mod-contents", "Lists all the files in a mod",
        new OptionDefinition[]
        {
            new OptionDefinition<ModListMarker>( "m", "managedGame", "The managed game instance that contains the mod"),
            new OptionDefinition<string>("n", "modName", "The name of the mod to list")
        });


    public async Task Run(ModListMarker managedGame, string modName)
    {
        var rows = new List<object[]>();
        var mod = managedGame.Value.Mods.First(m => m.Name == modName);
        foreach (var file in mod.Files)
        {
            if (file is FromArchive fa) 
                rows.Add(new object[]{fa.To, fa.From});
            else if (file is GameFile gf)
                rows.Add(new object[]{gf.To, gf.Installation});
                
        }

        await _renderer.Render(new Table(new[] { "Name", "Source"}, rows));
    }
}