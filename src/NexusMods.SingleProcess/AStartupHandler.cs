using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.SingleProcess.Exceptions;

namespace NexusMods.SingleProcess;

/// <summary>
/// A base class that can be used to implement <see cref="IStartupHandler"/>. This class will handle the creation of the
/// main process by calling the currently running executable again with the <see cref="IStartupHandler.MainProcessArgument"/>
/// </summary>
public abstract class AStartupHandler(ILogger logger, IFileSystem fileSystem) : IStartupHandler
{

    /// <inheritdoc />
    public abstract Task<int> HandleCliCommandAsync(string[] args, IRenderer renderer, CancellationToken token = default);

    /// <inheritdoc />
    public Task StartMainProcess()
    {
        var currentProcess = Process.GetCurrentProcess();
        logger.LogInformation("Starting main process from client process: {Process}", currentProcess.Id);
        var currentExecutable = fileSystem.FromUnsanitizedFullPath(currentProcess.MainModule!.FileName);

        ProcessStartInfo info;

        if (currentExecutable.GetFileNameWithoutExtension() == "dotnet")
        {
            logger.LogInformation("Starting main process with dotnet");
            info = new ProcessStartInfo
            {
                FileName = currentExecutable.ToString(),
                Arguments = $"{Environment.GetCommandLineArgs()[0]}",
                WorkingDirectory = Environment.CurrentDirectory,
                UseShellExecute = false
            };
        }
        else
        {
            logger.LogDebug("Starting main process");
            info = new ProcessStartInfo
            {
                FileName = currentExecutable.ToString(),
                UseShellExecute = true,
                CreateNoWindow = true,
            };
        }

        logger.LogInformation("Starting main process {FileName} with arguments: {Arguments}", info.FileName, info.Arguments);

        var process = new Process
        {
            StartInfo = info,

        };
        if (!process.Start())
            throw new MainProcessStartException(info.FileName, info.Arguments);

        logger.LogDebug("Main process started with PID: {PID}", process.Id);
        return Task.CompletedTask;
    }
}
