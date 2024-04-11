using CliWrap;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// OS interoperation for MacOS
/// </summary>
// ReSharper disable once InconsistentNaming
public class OSInteropOSX : IOSInterop
{
    private readonly IProcessFactory _processFactory;
    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="processFactory"></param>
    public OSInteropOSX(IProcessFactory processFactory)
    {
        _processFactory = processFactory;
    }

    /// <inheritdoc/>
    public async Task OpenUrl(Uri url, bool fireAndForget = false, CancellationToken cancellationToken = default)
    {
        var command = Cli.Wrap("open").WithArguments(url.ToString());
        var task = _processFactory.ExecuteAsync(command, cancellationToken);
        await task.AwaitOrForget(fireAndForget: fireAndForget, cancellationToken: cancellationToken);
    }
}
