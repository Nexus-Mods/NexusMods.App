using CliWrap;
using Microsoft.Extensions.Logging;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// OS interoperation for linux
/// </summary>
public class OSInteropLinux : AOSInterop
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public OSInteropLinux(ILoggerFactory loggerFactory, IProcessFactory processFactory)
        : base(loggerFactory, processFactory) { }

    /// <inheritdoc/>
    protected override Command CreateCommand(Uri uri)
    {
        return Cli.Wrap("xdg-open").WithArguments(new[] { uri.ToString() }, escape: true);
    }
}
