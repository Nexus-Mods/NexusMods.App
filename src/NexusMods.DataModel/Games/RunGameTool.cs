using System.Diagnostics;
using System.Text;
using CliWrap;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
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
    /// <param name="processFactory"></param>
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
    public async Task Execute(Loadout loadout, ApplyPlan applyPlan, CancellationToken cancellationToken)
    {
        var program = GetGamePath(loadout, applyPlan);
        _logger.LogInformation("Running {Program}", program);

        var primaryFile = _game.GetPrimaryFile(loadout.Installation.Store).CombineChecked(loadout.Installation);
        var names = new HashSet<string>()
        {
            program.FileName,
            program.GetFileNameWithoutExtension(),
            primaryFile.FileName,
            primaryFile.GetFileNameWithoutExtension()
        };

        // In the case of a preloader, we need to wait for the actual game file to exit
        // before we completely exit this routine. So get a list of all the processes with a give
        // name at the start, after the preloader finishes find any other processes with the same set of
        // names, and then we wait for those to exit.

        // In the case of something like Skyrim this means we will start with loading skse64_loader.exe then
        // notice that SkyrimSE.exe is running and wait for that to exit.

        var existing = FindMatchingProcesses(names).Select(p => p.Id).ToHashSet();

        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        var command = new Command(program.ToString())
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
            .WithValidation(CommandResultValidation.None)
            .WithWorkingDirectory(program.Parent.ToString());


        var result = await _processFactory.ExecuteAsync(command, cancellationToken);
        if (result.ExitCode != 0)
            _logger.LogError("While Running {Filename} : {Error} {Output}", program, stdErr, stdOut);

        var newProcesses = FindMatchingProcesses(names)
            .Where(p => !existing.Contains(p.Id))
            .ToHashSet();

        if (newProcesses.Count > 0)
        {
            _logger.LogInformation("Waiting for {Count} processes to exit", newProcesses.Count);
            while (true)
            {
                await Task.Delay(500, cancellationToken);
                if (newProcesses.All(p => p.HasExited))
                    break;
            }
            _logger.LogInformation("All {Count} processes have exited", newProcesses.Count);
        }

        _logger.LogInformation("Finished running {Program}", program);
    }

    private static HashSet<Process> FindMatchingProcesses(HashSet<string> names)
    {
        return Process.GetProcesses()
            .Where(p => names.Contains(p.ProcessName))
            .ToHashSet();
    }

    /// <summary>
    /// Returns the path to the main executable file for the game.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="applyPlan"></param>
    /// <returns></returns>
    protected virtual AbsolutePath GetGamePath(Loadout loadout, ApplyPlan applyPlan)
    {
        return _game.GetPrimaryFile(loadout.Installation.Store).Combine(loadout.Installation.LocationsRegister[LocationId.Game]);
    }
}
