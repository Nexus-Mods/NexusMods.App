using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts.Markers;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Lists the history of a loadout
/// </summary>
public class ListHistory : AVerb<LoadoutMarker>, IRenderingVerb
{
    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <inheritdoc />
    public static VerbDefinition Definition => new("list-history", "Lists the history of a loadout",
        new OptionDefinition[]
        {
            new OptionDefinition<LoadoutMarker>("l", "loadout", "Loadout to load")
        });

    /// <inheritdoc />
    public async Task<int> Run(LoadoutMarker loadout, CancellationToken token)
    {
        var rows = loadout.History()
            .Select(list => new object[] { list.LastModified, list.ChangeMessage, list.Mods.Count, list.DataStoreId })
            .ToList();

        await Renderer.Render(new Table(new[] { "Date", "Change Message", "Mod Count", "Id" }, rows));
        return 0;
    }
}
