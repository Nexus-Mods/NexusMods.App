using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.ModLists;

namespace NexusMods.CLI.Verbs;

public class ListManagedGames
{
    private readonly ModListManager _manager;
    private readonly IRenderer _renderer;

    public ListManagedGames(ModListManager manager, Configurator configurator)
    {
        _manager = manager;
        _renderer = configurator.Renderer;
    }
    public static VerbDefinition Definition = new VerbDefinition("list-managed-games",
        "List all the managed game instances (modlists) in the app",
        Array.Empty<OptionDefinition>());


    public async Task Run()
    {
        var rows = new List<object[]>();
        foreach (var list in _manager.AllModLists.Select(x => x.Value))
        {
            rows.Add(new object[]{list.Name, list.Installation, list.ModListId, list.Mods.Count});
        }

        await _renderer.Render(new Table(new[] { "Name", "Game", "Id", "Mod Count" }, rows));
    }
}