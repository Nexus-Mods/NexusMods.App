using System.Diagnostics;
using System.Globalization;
using System.Text;
using CliWrap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.Stores.Steam;
using NexusMods.Abstractions.Loadouts;
using NexusMods.CrossPlatform.Process;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Games;


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
    private readonly IOSInterop _osInterop;
    
    /// <summary>
    /// Whether this tool should be started through the shell instead of directly.
    /// This allows tools to start their own console, allowing users to interact with it.
    /// </summary>
    public virtual bool UseShell { get; set; } = false;
    
    
    /// <summary>
    /// Constructor
    /// </summary>
    public RunGameTool(IServiceProvider serviceProvider, T game)
    {
        _game = game;
        _logger = serviceProvider.GetRequiredService<ILogger<RunGameTool<T>>>();
        _processFactory = serviceProvider.GetRequiredService<IProcessFactory>();
        _osInterop = serviceProvider.GetRequiredService<IOSInterop>();
    }

    /// <inheritdoc />
    public IEnumerable<GameDomain> Domains => new[] { _game.Domain };

    /// <inheritdoc />
    public string Name => $"Run {_game.Name}";

    /// <inheritdoc />
    public async Task Execute(Loadout loadout, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {Name}", Name);

        var program = await GetGamePath(loadout);
        var primaryFile = _game.GetPrimaryFile(loadout.Installation.Store).CombineChecked(loadout.Installation);

        if (OSInformation.Shared.IsLinux && program.Equals(primaryFile) && loadout.Installation.LocatorResultMetadata is SteamLocatorResultMetadata steamLocatorResultMetadata)
        {
            await RunThroughSteam(steamLocatorResultMetadata.AppId, cancellationToken);
            return;
        }

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
            
        if (UseShell)
        {
            _logger.LogInformation("Running {Program} through shell", program);
            await RunWithShell(cancellationToken, program);
        }
        else
        {
            var result = await RunCommand(cancellationToken, program);
        }

        // Check if the process has spawned any new processes that we need to wait for (e.g. Launcher -> Game)
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

    private async Task<CommandResult> RunCommand(CancellationToken cancellationToken, AbsolutePath program)
    {
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
        return result;
    }

    private async Task<Process> RunWithShell(CancellationToken cancellationToken, AbsolutePath program)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = program.ToString(),
                WorkingDirectory = program.Parent.ToString(),
                UseShellExecute = true,
                CreateNoWindow = false,
            },
            EnableRaisingEvents = true,
        };
        
        try
        {
            await _processFactory.ExecuteProcessAsync(process, cancellationToken);
        }
        catch (Exception e)
        {
            if (!cancellationToken.IsCancellationRequested)
                _logger.LogError(e, "While Running {Filename}", program);
        }
        
        if (process.ExitCode != 0)
            _logger.LogError("While Running {Filename}", program);
        return process;
    }

    private async Task RunThroughSteam(uint appId, CancellationToken cancellationToken)
    {
        var existingReaperProcesses = Process.GetProcessesByName("reaper");

        // https://developer.valvesoftware.com/wiki/Steam_browser_protocol
        await _osInterop.OpenUrl(new Uri($"steam://rungameid/{appId.ToString(CultureInfo.InvariantCulture)}"), cancellationToken);

        if (OSInformation.Shared.IsWindows)
        {
            // TODO:
        } else if (OSInformation.Shared.IsLinux)
        {
            var steam = await WaitForProcessToStart("steam", Array.Empty<Process>(), TimeSpan.FromMinutes(1), cancellationToken);
            if (steam is null) return;

            // NOTE(erri120): Reaper is a custom tool for cleaning up child processes
            // See https://github.com/sonic2kk/steamtinkerlaunch/wiki/Steam-Reaper for details.
            var reaper = await WaitForProcessToStart("reaper", existingReaperProcesses, TimeSpan.FromMinutes(1), cancellationToken);
            if (reaper is null) return;

            await reaper.WaitForExitAsync(cancellationToken);
        }
        else
        {
            throw OSInformation.Shared.CreatePlatformNotSupportedException();
        }
    }

    private async ValueTask<Process?> WaitForProcessToStart(
        string processName,
        Process[] existingProcesses,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = DateTime.UtcNow;
            while (!cancellationToken.IsCancellationRequested && start + timeout > DateTime.UtcNow)
            {
                var processes = Process.GetProcessesByName(processName);
                var target = processes.FirstOrDefault(x => existingProcesses.All(p => p.Id != x.Id));
                if (target is not null) return target;

                await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
            }

            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while waiting for process \"{Process}\" to start", processName);
            return null;
        }
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
    protected virtual ValueTask<AbsolutePath> GetGamePath(Loadout loadout)
    {
        return ValueTask.FromResult(_game.GetPrimaryFile(loadout.Installation.Store)
            .Combine(loadout.Installation.LocationsRegister[LocationId.Game]));
    }
}
