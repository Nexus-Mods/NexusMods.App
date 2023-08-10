using DynamicData;
using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Compute and potentially run the steps needed to apply a Loadout to a game folder
/// </summary>
public class Apply : AVerb<LoadoutMarker, bool, bool>, IRenderingVerb
{
    private readonly LoadoutSynchronizer _loadoutSyncronizer;

    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="loadoutSynchronizer"></param>
    public Apply(LoadoutSynchronizer loadoutSynchronizer)
    {
        _loadoutSyncronizer = loadoutSynchronizer;
    }

    /// <inheritdoc />
    public static VerbDefinition Definition => new("apply", "Compute the steps needed to apply a Loadout to a game folder", new OptionDefinition[]
    {
        new OptionDefinition<LoadoutMarker>("l", "loadout", "Loadout to apply"),
        new OptionDefinition<bool>("r", "run", "Run computed apply steps? (makes actual changes on disk, defaults to just printing the steps otherwise)"),
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
            await Renderer.Render(new Table(new[] { "Action", "Count", "Size" }, rows));
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
                    case GenerateFile gf:
                        rows.Add(new object[] { gf, gf.To, gf.Fingerprint, "-"});
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown step type ({step.GetType()}) encountered, this should never happen.");
                }
            }
            await Renderer.Render(new Table(new[] { "Action", "To", "Hash", "Size" }, rows));
        }

        if (run)
        {
            await Renderer.WithProgress(token, async () =>
            {
                await _loadoutSyncronizer.Apply(plan, token);
                return plan.Steps;
            });
        }
        return 0;
    }
}
