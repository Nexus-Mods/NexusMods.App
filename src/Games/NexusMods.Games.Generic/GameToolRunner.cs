using System.Globalization;
using System.Text;
using CliWrap;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Loadouts;
using NexusMods.CrossPlatform.Process;
using NexusMods.Paths;
namespace NexusMods.Games.Generic;

/// <summary>
/// This is a utility class for executing game tools with additional context.
///
/// Motivation: Depending on the Operating System and/or Store, we may want to inject
/// additional logic before executing tools.
///
/// Example:
/// - A user on Linux may need to launch a Windows tool, but may not have Wine installed.
///   In this case, we can use Steam's `proton` installation for the game.
/// </summary>
public class GameToolRunner
{
    private readonly IProcessFactory _processFactory;
    private readonly AggregateProtontricksDependency? _protontricks;
    private readonly IOSInterop _osInterop;

    /// <summary/>
    public GameToolRunner(IServiceProvider provider)
    {
        _processFactory = provider.GetRequiredService<IProcessFactory>();
        _protontricks = provider.GetService<AggregateProtontricksDependency>();
        _osInterop = provider.GetRequiredService<IOSInterop>();
    }

    /// <summary>
    /// Executes the given command that starts the process.
    /// </summary>
    /// <param name="loadout">The loadout for which to execute the command for.</param>
    /// <param name="logProcessOutput">Whether to log the process output.</param>
    /// <param name="cancellationToken">Allows you to cancel the task, killing the process prematurely.</param>
    /// <param name="executablePath">Path to the executable for the game tool.</param>
    /// <param name="arguments">The arguments to be passed to the process.</param>
    /// <param name="workingDirectory">The working directory to start the process in.</param>
    public async Task<CommandResult> ExecuteAsync(Loadout.ReadOnly loadout, AbsolutePath executablePath, IEnumerable<string> arguments, AbsolutePath? workingDirectory, bool logProcessOutput = true, CancellationToken cancellationToken = default)
    {
        var isLinux = FileSystem.Shared.OS.IsLinux;
        if (!isLinux)
            return await _processFactory.ExecuteAsync(MakeCommand(executablePath, arguments, workingDirectory), logProcessOutput, cancellationToken);
        
        // For Linux, if the user is managing the game via Steam, we can use proton (via protontricks)
        var install = loadout.InstallationInstance;
        if (install.Store == GameStore.Steam && _protontricks is not null 
            && install.LocatorResultMetadata is SteamLocatorResultMetadata steamLocatorResultMetadata)
        {
            var appId = steamLocatorResultMetadata.AppId;
            var command = MakeCommand(executablePath, arguments, workingDirectory);
            command = await _protontricks.MakeLaunchCommand(command, appId);
            return await _processFactory.ExecuteAsync(command, logProcessOutput, cancellationToken);
        }
        if (install.Store == GameStore.GOG 
            && install.LocatorResultMetadata is HeroicGOGLocatorResultMetadata gogLocatorResultMetadata)
        {
            // Assemble the new format Heroic launch string.
            // TODO: Support non-GOG stores.
            var idAsNeutralString = gogLocatorResultMetadata.Id.ToString(CultureInfo.InvariantCulture);
            var baseString = new StringBuilder($"heroic://launch?&appName={idAsNeutralString}&runner=gog");
            foreach (var argument in arguments)
            {
                baseString.Append("&arg=");
                baseString.Append(argument);
            }

            // Note(sewer):
            // We must not canonicalize else Uri injects a backslash after `launch` in string, breaking the command.
            var uri = new Uri(baseString.ToString(), new UriCreationOptions()
            {
                DangerousDisablePathAndQueryCanonicalization = true,
            });
            await _osInterop.OpenUrl(uri, fireAndForget: false, cancellationToken: cancellationToken, logOutput: true);
            return new CommandResult(0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        }

        return await _processFactory.ExecuteAsync(MakeCommand(executablePath, arguments, workingDirectory), logProcessOutput, cancellationToken);
    }

    private Command MakeCommand(AbsolutePath executablePath, IEnumerable<string> arguments, AbsolutePath? workingDirectory)
    {
        var workingDirString = "";
        if (workingDirectory == null)
            workingDirString = executablePath.Parent.ToString();

        return Cli.Wrap(executablePath.ToString())
            .WithArguments(arguments, true)
            .WithWorkingDirectory(workingDirString);
    }
}
