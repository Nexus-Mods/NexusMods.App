using NexusMods.Abstractions.CLI;
using NexusMods.DataModel.Loadouts.Extensions;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Compute and run the steps needed to apply a Loadout to a game folder
/// </summary>
public class Ingest : AVerb<LoadoutMarker>, IRenderingVerb
{
    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="loadoutSynchronizer"></param>
    public Ingest()
    {
    }

    /// <inheritdoc />
    public static VerbDefinition Definition => new("ingest", "Ingest changes from the game folders into the given loadout", new OptionDefinition[]
    {
        new OptionDefinition<LoadoutMarker>("l", "loadout", "Loadout ingest changes into"),
    });

    /// <inheritdoc />
    public async Task<int> Run(LoadoutMarker loadout, CancellationToken token)
    {
        var state = await Renderer.WithProgress(token,
            async () => await loadout.Value.Ingest());

        loadout.Alter("Ingest changes from the game folder", _ => state);

        await Renderer.Render($"Ingested game folder changes into {loadout.Value.Name}");

        return 0;
    }
}
