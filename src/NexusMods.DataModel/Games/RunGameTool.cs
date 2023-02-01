using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Loadouts;
using NexusMods.Games.Abstractions;
using NexusMods.Paths;

namespace NexusMods.DataModel.Games;

/// <summary>
/// A tool that creates a launcher tool for a given game
/// </summary>
/// <typeparam name="T"></typeparam>
public class RunGameTool<T> : ITool
where T : AGame
{
    private readonly ILogger<RunGameTool<T>> _logger;
    private readonly T _game;

    public RunGameTool(ILogger<RunGameTool<T>> logger, T game)
    {
        _game = game;
        _logger = logger;
    }

    public IEnumerable<GameDomain> Domains => new[] { _game.Domain };
    public async Task Execute(Loadout loadout)
    {
        var program = _game.PrimaryFile.RelativeTo(loadout.Installation.Locations[GameFolderType.Game]);
        _logger.LogInformation("Running {Program}", program);
        
        var psi = new ProcessStartInfo(program.ToString())
        {

        };
        var process = Process.Start(psi);
        await process.WaitForExitAsync();
        _logger.LogInformation("Finished running {Program}", program);
    }

    public string Name => $"Run {_game.Name}";
}