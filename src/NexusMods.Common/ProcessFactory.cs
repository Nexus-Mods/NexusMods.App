using System.Diagnostics;
using CliWrap;

namespace NexusMods.Common;

/// <summary>
/// concrete implementation of <see cref="IProcessFactory"/> using actual os processes
/// </summary>
public class ProcessFactory : IProcessFactory
{
    /// <inheritdoc />
    public async Task<CommandResult> ExecuteAsync(Command command,
        CancellationToken cancellationToken = default)
    {
        return await command.ExecuteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Process? ExecuteAndDetach(Command command)
    {
        var info = new ProcessStartInfo(command.TargetFilePath)
        {
            Arguments = command.Arguments,
            WorkingDirectory = command.WorkingDirPath,

        };
        return Process.Start(info);
    }
}
