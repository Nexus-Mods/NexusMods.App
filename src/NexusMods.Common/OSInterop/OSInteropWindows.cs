using CliWrap;

namespace NexusMods.Common.OSInterop;

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
    public async Task OpenUrl(string url, CancellationToken cancellationToken = default)
    {
        // cmd /c start "" "https://google.com"
        var command = Cli.Wrap("cmd.exe").WithArguments($@"/c start """" ""{url}""");
        await _processFactory.ExecuteAsync(command, cancellationToken);
    }
}
