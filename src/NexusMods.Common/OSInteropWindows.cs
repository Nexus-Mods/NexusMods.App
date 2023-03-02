using CliWrap;

namespace NexusMods.Common;

/// <summary>
/// OS interoperation for windows
/// </summary>
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
    public async Task OpenURL(string url, CancellationToken cancellationToken = default)
    {
        var command = Cli.Wrap("explorer.exe").WithArguments(url);
        await _processFactory.ExecuteAsync(command, cancellationToken);
    }
}
