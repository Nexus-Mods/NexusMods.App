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
}
