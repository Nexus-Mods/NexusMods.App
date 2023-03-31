using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
public class ListManagedGames : AVerb
{
    private readonly LoadoutManager _manager;
    private readonly IRenderer _renderer;

    public ListManagedGames(LoadoutManager manager, Configurator configurator)
    {
        _manager = manager;
        _renderer = configurator.Renderer;
    }
    public static VerbDefinition Definition => new("list-managed-games",
        "List all the managed game instances (Loadouts) in the app",
        Array.Empty<OptionDefinition>());

    public async Task<int> Run(CancellationToken token)
    {
        var rows = new List<object[]>();
        foreach (var list in _manager.Registry.AllLoadouts())
            rows.Add(new object[] { list.Name, list.Installation, list.LoadoutId, list.Mods.Count });

        await _renderer.Render(new Table(new[] { "Name", "Game", "Id", "Mod Count" }, rows));

        return 0;
    }
}
