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
        var tcs = new TaskCompletionSource();
        
        process.EnableRaisingEvents = true;
        var hasExited = false;

        process.Exited += (sender, args) =>
        {
            hasExited = true;
            tcs.SetResult();
            process.Dispose();
        };
        
        cancellationToken.Register(() =>
        {
            if (hasExited) return;
            try
            {
                _logger.LogInformation("Killing process `{Process}`", process.StartInfo.FileName);
                process.Kill();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to kill process `{Process}`", process.StartInfo.FileName);
                tcs.SetException(e);
            }
        });

        try
        {
            _logger.LogInformation("Executing process `{Process}`", process.StartInfo.FileName);
            process.Start();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start process `{Process}`", process.StartInfo.FileName);
            tcs.SetException(e);
        }

        return tcs.Task; 
    }
}
