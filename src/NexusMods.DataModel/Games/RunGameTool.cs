using System.Diagnostics;
using System.Text;
using CliWrap;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.DataModel.Games;


/// <summary>
/// Marker interface for RunGameTool
/// </summary>
public interface IRunGameTool : ITool
{
}

/// <summary>
/// A tool that launches the game, using first found installation.
/// </summary>
/// <typeparam name="T"></typeparam>
public class RunGameTool<T> : IRunGameTool
where T : AGame
{
    private readonly ILogger<RunGameTool<T>> _logger;
    private readonly T _game;
    private readonly IProcessFactory _processFactory;

    /// <summary/>
    /// <param name="logger">The logger used to log execution.</param>
    /// <param name="game">The game to execute.</param>
    /// <remarks>
    ///    This constructor is usually called from DI.
    /// </remarks>
    public RunGameTool(ILogger<RunGameTool<T>> logger, T game, IProcessFactory processFactory)
    {
        _processFactory = processFactory;
        _game = game;
        _logger = logger;
    }

    /// <inheritdoc />
    public IEnumerable<GameDomain> Domains => new[] { _game.Domain };

    /// <inheritdoc />
    public string Name => $"Run {_game.Name}";

    /// <inheritdoc />
    public async Task Execute(Loadout loadout)
    {
        var program = _game.GetPrimaryFile(loadout.Installation.Store).Combine(loadout.Installation.Locations[GameFolderType.Game]);
        _logger.LogInformation("Running {Program}", program);

        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        var command = new Command(program.ToString())
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
            .WithValidation(CommandResultValidation.None)
            .WithWorkingDirectory(program.Parent.ToString());
        

        var result = await _processFactory.ExecuteAsync(command);
        if (result.ExitCode != 0)
            _logger.LogError("While Running {Filename} : {Error} {Output}", program, stdErr, stdOut);

        _logger.LogInformation("Finished running {Program}", program);
    }
}
