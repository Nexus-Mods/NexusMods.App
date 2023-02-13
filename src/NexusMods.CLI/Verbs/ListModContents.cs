using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.Loadouts.ModFiles;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
public class ListModContents : AVerb<LoadoutMarker, string>
{
    private readonly IRenderer _renderer;
    public ListModContents(Configurator configurator) => _renderer = configurator.Renderer;

    public static VerbDefinition Definition => new("list-mod-contents", "Lists all the files in a mod",
        new OptionDefinition[]
        {
            new OptionDefinition<LoadoutMarker>( "l", "loadout", "The loadout instance that contains the mod"),
            new OptionDefinition<string>("n", "modName", "The name of the mod to list")
        });
    
    public async Task<int> Run(LoadoutMarker loadout, string modName, CancellationToken token)
    {
        var rows = new List<object[]>();
        var mod = loadout.Value.Mods.Values.First(m => m.Name == modName);
        foreach (var file in mod.Files.Values)
        {
            if (file is FromArchive fa) 
                rows.Add(new object[]{fa.To, fa.From});
            else if (file is GameFile gf)
                rows.Add(new object[]{gf.To, gf.Installation});
        }

        await _renderer.Render(new Table(new[] { "Name", "Source"}, rows));
        return 0;
    }
}