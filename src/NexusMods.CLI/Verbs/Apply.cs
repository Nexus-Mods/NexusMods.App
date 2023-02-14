﻿using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
public class Apply : AVerb<LoadoutMarker, bool, bool>
{
    private readonly IRenderer _renderer;
    
    public Apply(Configurator configurator)
    {
        _renderer = configurator.Renderer;
    }

    public static VerbDefinition Definition => new("apply", "Apply a Loadout to a game folder", new OptionDefinition[]
    {
        new OptionDefinition<LoadoutMarker>("l", "loadout", "Loadout to apply"),
        new OptionDefinition<bool>("r", "run", "Run the application? (defaults to just printing the steps)"),
        new OptionDefinition<bool>("s", "summary", "Print the summary, not the detailed step list")
    });
    
    public async Task<int> Run(LoadoutMarker loadout, bool run, bool summary, CancellationToken token)
    {

        var plan = await loadout.MakeApplyPlan(token);

        if (summary)
        {
            var rows = plan.Steps.GroupBy(s => s.GetType())
                .Select(g =>
                    new object[]
                    {
                        g.Key.Name, g.Count(), g.OfType<IStaticFileStep>().Aggregate((Size)0L, (o, n) => o + n.Size)
                    });
            await _renderer.Render(new Table(new[] { "Action", "Count", "Size"}, rows));
        }
        else
        {
            var rows = new List<object[]>();
            foreach (var step in plan.Steps)
            {
                if (step is IStaticFileStep smf)
                {
                    rows.Add(new object[]{step, step.To, smf.Hash, smf.Size});
                }
                else
                {
                    rows.Add(new object[]{step, step.To, "", ""});
                }
            
            }
            await _renderer.Render(new Table(new[] { "Action", "To", "Hash", "Size"}, rows));
        }

        if (run) {
            await _renderer.WithProgress(token, async () =>
            {
                await loadout.Apply(plan, token);
                return plan.Steps;
            });
        }
        return 0;
    }
}