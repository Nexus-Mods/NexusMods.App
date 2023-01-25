﻿using Microsoft.Extensions.Logging;
using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

public class ListGames : AVerb
{
    private readonly IEnumerable<IGame> _games;

    public static VerbDefinition Definition => new("list-games",
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
    

    protected override async Task<int> Run(CancellationToken token)
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