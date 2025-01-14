using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CliWrap;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// Base class for <see cref="IOSInterop"/> implementations.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal abstract class AOSInterop : IOSInterop
{
    private readonly ILogger _logger;
    private readonly IProcessFactory _processFactory;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected AOSInterop(ILoggerFactory loggerFactory, IProcessFactory processFactory)
    {
        _logger = loggerFactory.CreateLogger(nameof(IOSInterop));
        _processFactory = processFactory;
    }

    /// <summary>
    /// Create a command.
    /// </summary>
    protected abstract Command CreateCommand(Uri uri);

    /// <inheritdoc/>
    public virtual async Task OpenUrl(Uri url, bool logOutput = false, bool fireAndForget = false, CancellationToken cancellationToken = default)
    {
        var command = CreateCommand(url);

        // NOTE(erri120): don't log the process output of the browser
        var isWeb = url.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) || url.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
        var shouldLogOutput = logOutput && !isWeb;

        var task = _processFactory.ExecuteAsync(command, logProcessOutput: shouldLogOutput, cancellationToken: cancellationToken);

        try
        {
            await task.AwaitOrForget(_logger, fireAndForget: fireAndForget, cancellationToken: cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // ignored
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while opening `{Uri}`", url);
        }
    }

    /// <inheritdoc />
    public virtual Task OpenFile(AbsolutePath filePath, bool logOutput = false, bool fireAndForget = false, CancellationToken cancellationToken = default)
    {
        if (!filePath.FileExists)
        {
            _logger.LogError("Unable to open file that doesn't exist at `{Path}`", filePath);
            return Task.CompletedTask;
        }

        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public virtual Task OpenDirectory(AbsolutePath directoryPath, bool logOutput = false, bool fireAndForget = true, CancellationToken cancellationToken = default)
    {
        if (!directoryPath.DirectoryExists())
        {
            _logger.LogError("Unable to open directory that doesn't exist at `{Path}`", directoryPath);
            return Task.CompletedTask;
        }

        return OpenUrl(new Uri($"file://{directoryPath.ToNativeSeparators(OSInformation.Shared)}"), logOutput, fireAndForget, cancellationToken);
    }

    public virtual Task OpenFileInDirectory(AbsolutePath filePath, bool logOutput = false, bool fireAndForget = true, CancellationToken cancellationToken = default)
    {
        return OpenDirectory(filePath, logOutput, fireAndForget, cancellationToken);
    }

    /// <inheritdoc />
    public virtual AbsolutePath GetOwnExe()
    {
        var processPath = Environment.ProcessPath;
        Debug.Assert(processPath is not null);

        return FileSystem.Shared.FromUnsanitizedFullPath(processPath);
    }
}
