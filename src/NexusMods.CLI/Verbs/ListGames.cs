using Microsoft.Extensions.Logging;
using NexusMods.CLI.DataOutputs;
using NexusMods.Interfaces.Components;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

public class ListGames
{
    private readonly IEnumerable<IGame> _games;

    public static readonly VerbDefinition Definition = new("list-games",
        "Lists all the installed games",
        Array.Empty<OptionDefinition>());

    private readonly ILogger<ListGames> _logger;
    private readonly IRenderer _renderer;


    public ListGames(ILogger<ListGames> logger, IEnumerable<IGame> games, Configurator configurator)
    {
        _logger = logger;
        _games = games;
        _renderer = configurator.Renderer;
    }
    

    public async Task<int> Run()
    {
        var installs = from game in _games.OrderBy(g => g.Name)
            from install in game.Installations.OrderBy(g => g.Version)
            select install;
        await _renderer.Render(new Table(new[] { "Game", "Version", "Path" },
            installs.Select(i => new object[]
            { i.Game, i.Version, i.Locations[GameFolderType.Game]})));
        return 0;
    }
}