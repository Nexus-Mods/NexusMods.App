using CliWrap;
using Microsoft.Extensions.Logging;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// concrete implementation of <see cref="IProcessFactory"/> using actual os processes
/// </summary>
public class ProcessFactory : IProcessFactory
{
    private readonly ILogger _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ProcessFactory(ILogger<ProcessFactory> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CommandResult> ExecuteAsync(Command command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing command `{Command}`", command.ToString());
        return await command.ExecuteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task ExecuteProcessAsync(System.Diagnostics.Process process, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.LongRunning);

        process.Exited += (sender, args) =>
        {
            tcs.SetResult();
            process.Dispose();
        };
        
        cancellationToken.Register(() =>
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        });

        process.Start();

        return tcs.Task; 
    }
}
