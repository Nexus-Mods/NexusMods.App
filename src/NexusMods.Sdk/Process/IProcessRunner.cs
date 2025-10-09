using CliWrap;
using JetBrains.Annotations;

namespace NexusMods.Sdk;

/// <summary>
/// Process abstraction.
/// </summary>
[PublicAPI]
public interface IProcessRunner
{
    /// <summary>
    /// Runs a command.
    /// </summary>
    /// <remarks>
    /// This method exists immediately and doesn't wait for the process to exit.
    /// </remarks>
    void Run(Command command, bool logOutput = false);

    /// <summary>
    /// Runs a command.
    /// </summary>
    /// <returns>A task that completes when the spawned process exists.</returns>
    Task<CommandResult> RunAsync(Command command, bool logOutput = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a process.
    /// </summary>
    /// <returns>A task that completes when the spawned process exists.</returns>
    Task RunAsync(System.Diagnostics.Process process, CancellationToken cancellationToken = default);
}
