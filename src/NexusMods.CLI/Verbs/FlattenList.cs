using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.LoadoutSynchronizer;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Displays a loadout
/// </summary>
public class FlattenList : AVerb<LoadoutMarker>, IRenderingVerb
{
    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// DI constructor
    /// </summary>
    public FlattenList()
    {
    }

    /// <inheritdoc />
    public static VerbDefinition Definition => new("flatten-list",
        "Flatten a loadout into the projected filesystem", new OptionDefinition[]
        {
            new OptionDefinition<LoadoutMarker>("l", "loadout", "loadout to target")
        });

    /// <inheritdoc />
    public async Task<int> Run(LoadoutMarker loadout, CancellationToken token)
    {
        var rows = new List<object[]>();
        var synchronizer = loadout.Value.Installation.Game.Synchronizer as IStandardizedLoadoutSynchronizer;
        if (synchronizer == null)
        {
            await Renderer.Render($"{loadout.Value.Installation.Game.Name} does not support flattening loadouts");
            return -1;
        }

        var flattened = await synchronizer.LoadoutToFlattenedLoadout(loadout.Value);


        foreach (var (path, pair) in flattened.GetAllDescendentFiles())
            rows.Add(new object[] { pair!.Mod.Name, path });

        await Renderer.Render(new Table(new[] { "Mod", "To" }, rows));
        return 0;
    }
}
