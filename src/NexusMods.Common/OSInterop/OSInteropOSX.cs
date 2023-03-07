using CliWrap;

namespace NexusMods.Common.OSInterop;

/// <summary>
/// OS interoperation for MacOS
/// </summary>
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
    public async Task OpenURL(string url, CancellationToken cancellationToken = default)
    {
        var command = Cli.Wrap("open").WithArguments(url);
        await _processFactory.ExecuteAsync(command, cancellationToken);
    }
}
