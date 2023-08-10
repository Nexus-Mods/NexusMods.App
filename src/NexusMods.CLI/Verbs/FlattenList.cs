using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Displays a loadout
/// </summary>
public class FlattenList : AVerb<LoadoutMarker>, IRenderingVerb
{
    private readonly LoadoutSynchronizer _loadoutSyncronizer;

    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="loadoutSynchronizer"></param>
    public FlattenList(LoadoutSynchronizer loadoutSynchronizer)
    {
        _loadoutSyncronizer = loadoutSynchronizer;
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
        foreach (var (path, mod) in (await _loadoutSyncronizer.FlattenLoadout(loadout.Value)).Files)
            rows.Add(new object[] { mod.Mod.Name, path });

        await Renderer.Render(new Table(new[] { "Mod", "To" }, rows));
        return 0;
    }
}
