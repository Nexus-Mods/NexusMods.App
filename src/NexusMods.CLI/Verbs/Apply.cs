using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Loadouts.Extensions;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Compute and run the steps needed to apply a Loadout to a game folder
/// </summary>
public class Apply : AVerb<LoadoutMarker>, IRenderingVerb
{
    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="loadoutSynchronizer"></param>
    public Apply()
    {
    }

    /// <inheritdoc />
    public static VerbDefinition Definition => new("apply", "Compute the steps needed to apply a Loadout to a game folder", new OptionDefinition[]
    {
        new OptionDefinition<LoadoutMarker>("l", "loadout", "Loadout to apply"),
    });

    /// <inheritdoc />
    public async Task<int> Run(LoadoutMarker loadout, CancellationToken token)
    {
        var state = await Renderer.WithProgress(token,
            async () => await loadout.Value.Apply());

        var summary = state.GetAllDescendentFiles()
            .Aggregate((Count:0, Size:Size.Zero), (acc, file) => (acc.Item1 + 1, acc.Item2 + file.Value.Size));

        await Renderer.Render($"Applied {loadout.Value.Name} resulting state contains {summary.Count} files and {summary.Size} of data");

        return 0;
    }
}
