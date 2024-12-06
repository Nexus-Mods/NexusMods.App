using System.Runtime.Versioning;
using CliWrap;
using Microsoft.Extensions.Logging;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// OS interoperation for MacOS
/// </summary>
// ReSharper disable once InconsistentNaming
[SupportedOSPlatform("macos")]
internal class OSInteropOSX : AOSInterop
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public OSInteropOSX(ILoggerFactory loggerFactory, IProcessFactory processFactory)
        : base(loggerFactory, processFactory) { }

    /// <inheritdoc/>
    protected override Command CreateCommand(Uri uri)
    {
        var uriString = uri.ToString().Replace(" ", "%20");
        return Cli.Wrap("open").WithArguments(uriString);
    }


}
