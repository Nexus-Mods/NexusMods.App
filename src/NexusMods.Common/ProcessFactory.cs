using CliWrap;

namespace NexusMods.Common;

/// <summary>
/// concrete implementation of <see cref="IProcessFactory"/> using actual os processes
/// </summary>
public class ProcessFactory : IProcessFactory
{
    public async Task<CommandResult> ExecuteAsync(Command command,
        CancellationToken cancellationToken = default)
    {
        return await command.ExecuteAsync(cancellationToken);
    }
}
