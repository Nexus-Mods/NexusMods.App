using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.ModLists.ApplySteps;
using NexusMods.DataModel.ModLists.Markers;
using NexusMods.DataModel.ModLists.ModFiles;

namespace NexusMods.CLI.Verbs;

public class Apply
{
    private readonly IRenderer _renderer;
    public Apply(Configurator configurator)
    {
        _renderer = configurator.Renderer;
    }

    public static VerbDefinition Definition => new VerbDefinition("apply", "Apply a modlist to a game folder", new OptionDefinition[]
    {
        new OptionDefinition<ModListMarker>("m", "modList", "Mod List to apply"),
        new OptionDefinition<bool>("r", "run", "Run the application? (defaults to just printing the steps)")
    });
    
    public async Task Run(ModListMarker modList, bool run, CancellationToken token)
    {
        var rows = new List<object[]>();
        await foreach (var step in modList.MakeApplyPlan(token))
        {
            if (step is IStaticFileStep smf)
            {
                rows.Add(new object[]{step, step.To, smf.Hash, smf.Size});
            }
            else
            {
                rows.Add(new object[]{step, step.To, null, default});
            }
            
        }

        await _renderer.Render(new Table(new[] { "Action", "To", "Hash", "Size"}, rows));
    }
}