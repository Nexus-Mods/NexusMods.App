using DynamicData;
using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Apply a Loadout to a game folder
/// </summary>
public class Apply : AVerb<LoadoutMarker, bool, bool>
{
    private readonly IRenderer _renderer;
    private readonly LoadoutSynchronizer _loadoutSyncronizer;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="loadoutSynchronizer"></param>
    public Apply(Configurator configurator, LoadoutSynchronizer loadoutSynchronizer)
    {
        _renderer = configurator.Renderer;
        _loadoutSyncronizer = loadoutSynchronizer;
    }

    /// <inheritdoc />
    public static VerbDefinition Definition => new("apply", "Apply a Loadout to a game folder", new OptionDefinition[]
    {
        new OptionDefinition<LoadoutMarker>("l", "loadout", "Loadout to apply"),
        new OptionDefinition<bool>("r", "run", "Run the application? (defaults to just printing the steps)"),
        new OptionDefinition<bool>("s", "summary", "Print the summary, not the detailed step list")
    });

    /// <inheritdoc />
    public async Task<int> Run(LoadoutMarker loadout, bool run, bool summary, CancellationToken token)
    {

        var plan = await _loadoutSyncronizer.MakeApplySteps(loadout.Value, token);

        if (summary)
        {
            var rows = plan.Steps.GroupBy(s => s.GetType())
                .Select(g =>
                    new object[]
                    {
                        g.Key.Name, g.Count(), g.OfType<IStaticFileStep>().Aggregate((Size)0L, (o, n) => o + n.Size)
                    });
            await _renderer.Render(new Table(new[] { "Action", "Count", "Size" }, rows));
        }
        else
        {
            var rows = new List<object[]>();
            foreach (var step in plan.Steps)
            {
                switch (step)
                {
                    case ExtractFile ef:
                        rows.Add(new object[] { ef, ef.To, ef.Hash, ef.Size});
                        break;
                    case BackupFile bf:
                        rows.Add(new object[] { bf, bf.To, bf.Hash, bf.Size});
                        break;
                    case DeleteFile df:
                        rows.Add(new object[] { df, df.To, df.Hash, df.Size });
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown step type ({step.GetType()}) encountered, this should never happen.");
                }
            }
            await _renderer.Render(new Table(new[] { "Action", "To", "Hash", "Size" }, rows));
        }

        if (run)
        {
            await _renderer.WithProgress(token, async () =>
            {
                await _loadoutSyncronizer.Apply(plan, token);
                return plan.Steps;
            });
        }
        return 0;
    }
}
