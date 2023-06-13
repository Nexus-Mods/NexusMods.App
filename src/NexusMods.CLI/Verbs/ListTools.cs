using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Games;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// List all tools
/// </summary>
public class ListTools : AVerb
{
    private readonly IRenderer _renderer;
    private readonly IEnumerable<ITool> _tools;

    /// <inheritdoc />
    public static VerbDefinition Definition => new("list-tools",
        "List all tools", Array.Empty<OptionDefinition>());

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="tools"></param>
    public ListTools(Configurator configurator, IEnumerable<ITool> tools)
    {
        _renderer = configurator.Renderer;
        _tools = tools;
    }

    /// <inheritdoc />
    public async Task<int> Run(CancellationToken token)
    {
        await _renderer.Render(new Table(new[] { "Name", "Games" },
            _tools.Select(t => new object[] { t.Name, string.Join(", ", t.Domains) })));
        return 0;
    }
}
