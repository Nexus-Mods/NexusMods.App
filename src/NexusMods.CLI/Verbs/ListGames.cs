using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
public class ListGames : AVerb
{
    private readonly IEnumerable<IGame> _games;

    public static VerbDefinition Definition => new("list-games",
        "Lists all the installed games",
        Array.Empty<OptionDefinition>());

    private readonly IRenderer _renderer;

    public ListGames(IEnumerable<IGame> games, Configurator configurator)
    {
        _games = games;
        _renderer = configurator.Renderer;
    }
    
    public async Task<int> Run(CancellationToken token)
    {
        var installs = from game in _games.OrderBy(g => g.Name)
            from install in game.Installations.OrderBy(g => g.Version)
            select install;
        
        await _renderer.Render(new Table(new[] { "Game", "Version", "Path", "Store" },
            installs.Select(i => new object[] { i.Game, i.Version, i.Locations[GameFolderType.Game], i.Store })));
        
        return 0;
    }
}