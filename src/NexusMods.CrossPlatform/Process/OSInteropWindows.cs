using CliWrap;
using Microsoft.Extensions.Logging;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// OS interoperation for windows
/// </summary>
// ReSharper disable once InconsistentNaming
public class OSInteropWindows : AOSInterop
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public OSInteropWindows(ILoggerFactory loggerFactory, IProcessFactory processFactory)
        : base(loggerFactory, processFactory) { }

    /// <inheritdoc/>
    protected override Command CreateCommand(Uri uri)
    {
        // cmd /c start "" "https://google.com"
        return Cli.Wrap("cmd.exe").WithArguments($@"/c start """" ""{uri}""");
    }
}
