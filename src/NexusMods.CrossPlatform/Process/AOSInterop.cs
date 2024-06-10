using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CliWrap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// Base class for <see cref="IOSInterop"/> implementations.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public abstract class AOSInterop : IOSInterop
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
    public async Task OpenUrl(Uri url, bool fireAndForget = false, CancellationToken cancellationToken = default)
    {
        var command = CreateCommand(url);

        // NOTE(erri120): don't log the process output of the browser
        var isWeb = url.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) || url.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
        var task = _processFactory.ExecuteAsync(command, logProcessOutput: !isWeb, cancellationToken: cancellationToken);

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
    public virtual AbsolutePath GetOwnExe()
    {
        var processPath = Environment.ProcessPath;
        Debug.Assert(processPath is not null);

        return FileSystem.Shared.FromUnsanitizedFullPath(processPath);
    }
}
