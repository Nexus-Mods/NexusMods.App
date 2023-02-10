using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Games;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
public class ListTools : AVerb
{
    private readonly IRenderer _renderer;
    private readonly IEnumerable<ITool> _tools;
    
    public static VerbDefinition Definition => new("list-tools",
        "List all tools", Array.Empty<OptionDefinition>());
    
    public ListTools(Configurator configurator, IEnumerable<ITool> tools)
    {
        _renderer = configurator.Renderer;
        _tools = tools;
    }
    
    public async Task<int> Run(CancellationToken token)
    {
        await _renderer.Render(new Table(new[] { "Name", "Games" },
            _tools.Select(t => new object[] { t.Name, string.Join(", ", t.Domains) })));
        return 0;
    }
}