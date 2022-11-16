using Microsoft.Extensions.Logging;
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

    
    public ListGames(ILogger<ListGames> logger, IEnumerable<IGame> games)
    {
        _logger = logger;
        _games = games;
    }
    

    public async Task<int> Run()
    {
        var installs = from game in _games.OrderBy(g => g.Name)
            from install in game.Installations.OrderBy(g => g.Version)
            select install;
        foreach (var install in installs)
        {
            _logger.LogInformation("{Install} at {Path}", install, install.Locations[GameFolderType.Game]);
        }

        return 0;
    }
}