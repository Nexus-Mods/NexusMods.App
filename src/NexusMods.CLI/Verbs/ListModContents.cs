using DynamicData;
using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.Loadouts.ModFiles;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Lists all the files in a mod
/// </summary>
public class ListModContents : AVerb<LoadoutMarker, string>
{
    private readonly IRenderer _renderer;
    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="configurator"></param>
    public ListModContents(Configurator configurator) => _renderer = configurator.Renderer;

    /// <inheritdoc />
    public static VerbDefinition Definition => new("list-mod-contents", "Lists all the files in a mod",
        new OptionDefinition[]
        {
            new OptionDefinition<LoadoutMarker>( "l", "loadout", "The loadout instance that contains the mod"),
            new OptionDefinition<string>("n", "modName", "The name of the mod to list")
        });

    /// <inheritdoc />
    public async Task<int> Run(LoadoutMarker loadout, string modName, CancellationToken token)
    {

        var rows = new List<object[]>();
        var mod = loadout.Value.Mods.Values.First(m => m.Name == modName);
        foreach (var file in mod.Files.Values)
        {
            if (file is IToFile tf and IFromArchive fa)
            {
                rows.Add(new object[] { tf.To, fa.Hash });
            }
            else if (file is IToFile tf2 and IGeneratedFile gf)
                rows.Add(new object[] { tf2, gf.GetType().ToString() });
            else 
                rows.Add(new object[] { file.GetType().ToString() });
        }

        await _renderer.Render(new Table(new[] { "Name", "Source" }, rows));
        return 0;
    }
}
