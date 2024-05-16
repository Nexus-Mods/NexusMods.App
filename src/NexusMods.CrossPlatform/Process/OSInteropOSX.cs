using CliWrap;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// OS interoperation for MacOS
/// </summary>
// ReSharper disable once InconsistentNaming
public class OSInteropOSX : AOSInterop
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public OSInteropOSX(ILoggerFactory loggerFactory, IProcessFactory processFactory)
        : base(loggerFactory, processFactory) { }

    /// <inheritdoc/>
    protected override Command CreateCommand(Uri uri)
    {
        return Cli.Wrap("open").WithArguments(uri.ToString());
    }


}
