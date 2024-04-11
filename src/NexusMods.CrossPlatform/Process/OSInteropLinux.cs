using CliWrap;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// OS interoperation for linux
/// </summary>
// ReSharper disable once InconsistentNaming
public class OSInteropLinux : IOSInterop
{
    private readonly IProcessFactory _processFactory;
    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="processFactory"></param>
    public OSInteropLinux(IProcessFactory processFactory)
    {
        _processFactory = processFactory;
    }

    /// <inheritdoc/>
    public async Task OpenUrl(Uri url, bool fireAndForget = false, CancellationToken cancellationToken = default)
    {
        var command = Cli.Wrap("xdg-open").WithArguments(new[] { url.ToString() }, escape: true);
        var task = _processFactory.ExecuteAsync(command, cancellationToken);

        if (fireAndForget)
        {
            task.Start(TaskScheduler.Default);
        }
        else
        {
            await task;
        }
    }
}
