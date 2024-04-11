using CliWrap;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// OS interoperation for windows
/// </summary>
// ReSharper disable once InconsistentNaming
public class OSInteropWindows : IOSInterop
{
    private readonly IProcessFactory _processFactory;
    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="processFactory"></param>
    public OSInteropWindows(IProcessFactory processFactory)
    {
        _processFactory = processFactory;
    }

    /// <inheritdoc/>
    public async Task OpenUrl(Uri url, bool fireAndForget = false, CancellationToken cancellationToken = default)
    {
        // cmd /c start "" "https://google.com"
        var command = Cli.Wrap("cmd.exe").WithArguments($@"/c start """" ""{url}""");
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
