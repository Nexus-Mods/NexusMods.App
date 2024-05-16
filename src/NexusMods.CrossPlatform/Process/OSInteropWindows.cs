using CliWrap;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// OS interoperation for windows
/// </summary>
// ReSharper disable once InconsistentNaming
public class OSInteropWindows : AOSInterop
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Constructor.
    /// </summary>
    public OSInteropWindows(ILoggerFactory loggerFactory, IProcessFactory processFactory, IFileSystem fileSystem)
        : base(loggerFactory, processFactory)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    protected override Command CreateCommand(Uri uri)
    {
        // cmd /c start "" "https://google.com"
        return Cli.Wrap("cmd.exe").WithArguments($@"/c start """" ""{uri}""");
    }
    
}
