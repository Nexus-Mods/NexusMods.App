using System.Diagnostics;
using CliWrap;

namespace NexusMods.Common;

/// <summary>
/// Process factory.
/// </summary>
public interface IProcessFactory
{
    /// <summary>
    /// Executes the given command that starts the process.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Allows you to cancel the task, killing the process prematurely.</param>
    Task<CommandResult> ExecuteAsync(Command command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the given command that starts the process, but remains detached from it in such a way
    /// that the process will continue to run even if the parent process is killed.
    ///
    /// Returns null if the process could not be started.
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    Process? ExecuteAndDetach(Command command);
}
