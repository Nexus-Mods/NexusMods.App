using CliWrap;

namespace NexusMods.CrossPlatform.Process;

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
}
