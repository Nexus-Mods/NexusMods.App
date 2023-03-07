using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.DataModel.Games;

/// <summary>
/// A tool that launches the game, using first found installation.
/// </summary>
/// <typeparam name="T"></typeparam>
public class RunGameTool<T> : ITool
where T : AGame
{
    private readonly ILogger<RunGameTool<T>> _logger;
    private readonly T _game;

    /// <summary/>
    /// <param name="logger">The logger used to log execution.</param>
    /// <param name="game">The game to execute.</param>
    /// <remarks>
    ///    This constructor is usually called from DI.
    /// </remarks>
    public RunGameTool(ILogger<RunGameTool<T>> logger, T game)
    {
        _game = game;
        _logger = logger;
    }

    public IEnumerable<GameDomain> Domains => new[] { _game.Domain };

    /// <inheritdoc />
    public string Name => $"Run {_game.Name}";

    /// <inheritdoc />
    public async Task Execute(Loadout loadout)
    {
        var program = _game.PrimaryFile.CombineChecked(loadout.Installation.Locations[GameFolderType.Game]);
        _logger.LogInformation("Running {Program}", program);

        // TODO: use IProcessFactory
        var psi = new ProcessStartInfo(program.ToString());
        var process = Process.Start(psi);
        await process!.WaitForExitAsync();

        _logger.LogInformation("Finished running {Program}", program);
    }
}
