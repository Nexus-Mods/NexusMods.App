using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts.Markers;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// List all the mods in a given managed game
/// </summary>
public class ListMods : AVerb<LoadoutMarker>, IRenderingVerb
{
    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <inheritdoc />
    public static VerbDefinition Definition => new("list-mods",
        "List all the mods in a given managed game",
        new OptionDefinition[]
        {
            new OptionDefinition<LoadoutMarker>("l", "loadout", "The managed game to access")
        });

    /// <inheritdoc />
    public async Task<int> Run(LoadoutMarker loadout, CancellationToken token)
    {
        var rows = new List<object[]>();
        foreach (var mod in loadout.Value.Mods.Values)
            rows.Add(new object[] { mod.Name, mod.Files.Count });

        await Renderer.Render(new Table(new[] { "Name", "File Count" }, rows));
        return 0;
    }
}
