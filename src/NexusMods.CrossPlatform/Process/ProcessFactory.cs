using CliWrap;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// concrete implementation of <see cref="IProcessFactory"/> using actual os processes
/// </summary>
public class ProcessFactory : IProcessFactory
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly AbsolutePath _processLogsFolder;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ProcessFactory(
        ILogger<ProcessFactory> logger,
        IFileSystem fileSystem,
        ISettingsManager settingsManager)
    {
        _logger = logger;
        _fileSystem = fileSystem;

        _processLogsFolder = LoggingSettings.GetLogBaseFolder(fileSystem.OS, fileSystem).Combine("ProcessLogs");
        _logger.LogInformation("Using process log folder at {Path}", _processLogsFolder);

        _processLogsFolder.CreateDirectory();

        CleanupOldLogFiles(logger, _processLogsFolder, settingsManager);
    }

    private static void CleanupOldLogFiles(ILogger logger, AbsolutePath processLogsFolder, ISettingsManager settingsManager)
    {
        var loggingSettings = settingsManager.Get<LoggingSettings>();
        var retentionSpan = loggingSettings.ProcessLogRetentionSpan;

        var filesToDelete = processLogsFolder
            .EnumerateFiles()
            .Where(x => DateTime.Now - x.FileInfo.CreationTime >= retentionSpan)
            .ToArray();

        foreach (var file in filesToDelete)
        {
            try
            {
                file.Delete();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to delete expired log file {Path}", file);
            }
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult> ExecuteAsync(
        Command command,
        bool logProcessOutput = true,
        CancellationToken cancellationToken = default)
    {
        if (!logProcessOutput)
        {
            return await ExecuteAsync(command, cancellationToken);
        }

        var fileName = GetFileName(command);

        var logFileName = $"{fileName}-{DateTime.Now:s}";
        var stdOutFilePath = _processLogsFolder.Combine(logFileName + ".stdout.log");
        var stdErrFilePath = _processLogsFolder.Combine(logFileName + ".stderr.log");
        _logger.LogInformation("Using process logs {StdOutLogPath} and {StdErrLogPath}", stdOutFilePath, stdErrFilePath);

        await using (var stdOutStream = stdOutFilePath.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
        await using (var stdErrStream = stdErrFilePath.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
        {
            var stdOutPipe = PipeTarget.ToStream(stdOutStream, autoFlush: true);
            var stdErrPipe = PipeTarget.ToStream(stdErrStream, autoFlush: true);

            var mergedStdOutPipe = command.StandardOutputPipe == PipeTarget.Null ? stdOutPipe : PipeTarget.Merge(command.StandardOutputPipe, stdOutPipe);
            var mergedStdErrPipe = command.StandardErrorPipe == PipeTarget.Null ? stdErrPipe : PipeTarget.Merge(command.StandardErrorPipe, stdErrPipe);

            command = command
                .WithStandardOutputPipe(mergedStdOutPipe)
                .WithStandardErrorPipe(mergedStdErrPipe);

            var result = await ExecuteAsync(command, cancellationToken);
            _logger.LogInformation("Command `{Command}` finished after {RunTime} seconds with exit Code {ExitCode}", command.ToString(), result.RunTime.TotalSeconds, result.ExitCode);

            return result;
        }
    }

    private string GetFileName(Command command)
    {
        if (PathHelpers.IsRooted(command.TargetFilePath, _fileSystem.OS))
        {
            return _fileSystem.FromUnsanitizedFullPath(command.TargetFilePath).FileName;
        }

        return new RelativePath(command.TargetFilePath).FileName.ToString();
    }

    private async Task<CommandResult> ExecuteAsync(Command command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing command `{Command}`", command.ToString());
        return await command.ExecuteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task ExecuteProcessAsync(System.Diagnostics.Process process, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource();
        
        process.EnableRaisingEvents = true;
        var hasExited = false;

        process.Exited += (_, _) =>
        {
            hasExited = true;
            tcs.SetResult();
            process.Dispose();
        };
        
        cancellationToken.Register(() =>
        {
            if (hasExited) return;
            try
            {
                _logger.LogInformation("Killing process `{Process}`", process.StartInfo.FileName);
                process.Kill();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to kill process `{Process}`", process.StartInfo.FileName);
                tcs.SetException(e);
            }
        });

        try
        {
            _logger.LogInformation("Executing process `{Process}`", process.StartInfo.FileName);
            process.Start();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start process `{Process}`", process.StartInfo.FileName);
            tcs.SetException(e);
        }

        return tcs.Task; 
    }
}
