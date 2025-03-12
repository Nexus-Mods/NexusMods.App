using System.Runtime.Versioning;
using CliWrap;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// OS interoperation for windows
/// </summary>
// ReSharper disable once InconsistentNaming
[SupportedOSPlatform("windows")]
internal class OSInteropWindows : AOSInterop
{
    private readonly IFileSystem _fileSystem;
    private readonly IProcessFactory _processFactory;
    private readonly ILogger _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public OSInteropWindows(
        ILoggerFactory loggerFactory,
        IProcessFactory processFactory,
        IFileSystem fileSystem) : base(loggerFactory, processFactory)
    {
        _fileSystem = fileSystem;
        _processFactory = processFactory;
        _logger = loggerFactory.CreateLogger<OSInteropWindows>();
    }

    /// <inheritdoc/>
    protected override Command CreateCommand(Uri uri)
    {
        // cmd /c start "" "https://google.com"
        return Cli.Wrap("cmd.exe").WithArguments($@"/c start """" ""{uri}""");
    }

    public override async Task OpenFileInDirectory(AbsolutePath filePath, bool logOutput = false, bool fireAndForget = true, CancellationToken cancellationToken = default)
    {
        var path = filePath.ToNativeSeparators(_fileSystem.OS);

        // reference: https://ss64.com/nt/explorer.html
        var command = Cli.Wrap("explorer.exe").WithArguments($@"/select,""{path}""")
            // NOTE(Al12rs): Apparently explorer.exe returns 1 on success here, so we need to ignore non-zero return values during validation
            .WithValidation(CommandResultValidation.None);
        
        try
        {
            var task = _processFactory.ExecuteAsync(command, logProcessOutput: logOutput, cancellationToken: cancellationToken);
            await task.AwaitOrForget(_logger, fireAndForget: fireAndForget, cancellationToken: cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // ignored
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while opening file `{FilePath}`", filePath);
        }
    }
}
