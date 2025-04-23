using System.Text;
using CliWrap;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
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

    /// <summary/>
    public GameToolRunner(IServiceProvider provider)
    {
        _processFactory = provider.GetRequiredService<IProcessFactory>();
        _protontricks = provider.GetService<AggregateProtontricksDependency>();
    }

    /// <summary>
    /// Executes the given command that starts the process.
    /// </summary>
    /// <param name="loadout">The loadout for which to execute the command for.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="logProcessOutput">Whether to log the process output.</param>
    /// <param name="cancellationToken">Allows you to cancel the task, killing the process prematurely.</param>
    public async Task<CommandResult> ExecuteAsync(Loadout.ReadOnly loadout, Command command, bool logProcessOutput = true, CancellationToken cancellationToken = default)
    {
        var isLinux = FileSystem.Shared.OS.IsLinux;
        if (!isLinux)
            return await _processFactory.ExecuteAsync(command, logProcessOutput, cancellationToken: cancellationToken);
        
        // For Linux, if the user is managing the game via Steam, we can use proton (via protontricks)
        var install = loadout.InstallationInstance;
        if (install.Store == GameStore.Steam && _protontricks is not null 
            && install.LocatorResultMetadata is SteamLocatorResultMetadata steamLocatorResultMetadata)
        {
            var appId = steamLocatorResultMetadata.AppId;
            command = await _protontricks.MakeLaunchCommand(command, appId);
            return await _processFactory.ExecuteAsync(command, logProcessOutput, cancellationToken: cancellationToken);
        }

        return await _processFactory.ExecuteAsync(command, logProcessOutput, cancellationToken: cancellationToken);
    }
}
