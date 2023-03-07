using CliWrap;

namespace NexusMods.Common;

/// <summary>
/// Process factory.
/// </summary>
public interface IProcessFactory
{
    Task<CommandResult> ExecuteAsync(Command command, CancellationToken cancellationToken = default);
}
