using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.DataModel.Games;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// List all tools
/// </summary>
public class ListTools : AVerb, IRenderingVerb
{
    private readonly IEnumerable<ITool> _tools;

    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <inheritdoc />
    public static VerbDefinition Definition => new("list-tools",
        "List all tools", Array.Empty<OptionDefinition>());

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="tools"></param>
    public ListTools(IEnumerable<ITool> tools)
    {
        _tools = tools;
    }

    /// <inheritdoc />
    public async Task<int> Run(CancellationToken token)
    {
        await Renderer.Render(new Table(new[] { "Name", "Games" },
            _tools.Select(t => new object[] { t.Name, string.Join(", ", t.Domains) })));
        return 0;
    }
}
