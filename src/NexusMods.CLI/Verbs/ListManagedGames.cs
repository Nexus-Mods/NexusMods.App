using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// List all the managed game instances (Loadouts) in the app
/// </summary>
public class ListManagedGames : AVerb, IRenderingVerb
{
    private readonly LoadoutRegistry _registry;

    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="registry"></param>
    public ListManagedGames(LoadoutRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    public static VerbDefinition Definition => new("list-managed-games",
        "List all the managed game instances (Loadouts) in the app",
        Array.Empty<OptionDefinition>());

    /// <inheritdoc />
    public async Task<int> Run(CancellationToken token)
    {
        var rows = new List<object[]>();
        foreach (var list in _registry.AllLoadouts())
            rows.Add(new object[] { list.Name, list.Installation, list.LoadoutId, list.Mods.Count });

        await Renderer.Render(new Table(new[] { "Name", "Game", "Id", "Mod Count" }, rows));

        return 0;
    }
}
