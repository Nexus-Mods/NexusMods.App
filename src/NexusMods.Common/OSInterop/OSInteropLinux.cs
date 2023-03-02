using CliWrap;

namespace NexusMods.Common.OSInterop;

/// <summary>
/// OS interoperation for linux
/// </summary>
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
    public async Task OpenURL(string url, CancellationToken cancellationToken = default)
    {
        var command = Cli.Wrap("xdg-open").WithArguments(url);
        await _processFactory.ExecuteAsync(command, cancellationToken);
    }
}
