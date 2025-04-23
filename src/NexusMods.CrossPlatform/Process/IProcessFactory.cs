using CliWrap;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// Process factory.
/// </summary>
public interface IProcessFactory
{
    /// <summary>
    /// Executes the given command that starts the process.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="logProcessOutput"></param>
    /// <param name="validateExitCode"></param>
    /// <param name="cancellationToken">Allows you to cancel the task, killing the process prematurely.</param>
    Task<CommandResult> ExecuteAsync(Command command, bool logProcessOutput = true, bool validateExitCode = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the gives process asynchronously.
    /// </summary>
    /// <remarks>
    /// Can be awaited to wait for the process to finish.
    /// </remarks>
    /// <param name="process">The process to execute</param>
    /// <param name="cancellationToken">Allows you to cancel the task, killing the process prematurely.</param>
    /// <returns>The process when it has exited</returns>
    Task ExecuteProcessAsync(System.Diagnostics.Process process, CancellationToken cancellationToken = default);
}
