using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.Sdk.Settings;

namespace NexusMods.SingleProcess;

/// <summary>
/// A shared file that is used to synchronize the main process and client processes.
/// </summary>
public class SyncFile
{
    private readonly MultiProcessSharedArray _sharedArray;
    private readonly ILogger<SyncFile> _logger;

    /// <summary>
    /// DI constructor
    /// </summary>
    public SyncFile(ILogger<SyncFile> logger, ISettingsManager settingsManager)
    {
        var settings = settingsManager.Get<CliSettings>();
        _logger = logger;
        _logger.LogDebug("Using Sync File: {SyncFile}", settings.SyncFile);
        _sharedArray = new MultiProcessSharedArray(settings.SyncFile, itemCount: 8);
    }
    
    /// <summary>
    /// True if the main process is running
    /// </summary>
    public bool IsMainRunning => GetSyncInfo().Process is not null;
    
    /// <summary>
    /// Returns the current process and port that's stored in the sync file
    /// </summary>
    /// <returns></returns>
    public (Process? Process, int Port) GetSyncInfo()
    {
        var rawValue = _sharedArray.Get(0);
        var processId = (int)(rawValue >> 32);
        var port = (int) rawValue;
        
        
        _logger.LogDebug("Sync file contents: {Port}, {ProcessId}", port, processId);

        if (processId == 0) return (null, port);
        var process = GetProcessById(processId);
        return (process, port);
    }

    /// <summary>
    /// Flags this process as the main process, returns false if another process is already the main process
    /// </summary>
    public bool TrySetMain(int port)
    {
        var newProcessId = Environment.ProcessId;
        var newContents = ((ulong)newProcessId << 32) | (uint) port;


        var currentRawValue = _sharedArray.Get(0);
        var currentProcessId = (int)(currentRawValue >> 32);

        var process = GetProcessById(currentProcessId);
        if (process is not null && process.Id != newProcessId)
            return false;

        return _sharedArray.CompareAndSwap(0, currentRawValue, newContents);
    }
    
    /// <summary>
    /// Returns the process with the given id, or null if it doesn't exist
    /// </summary>
    private static Process? GetProcessById(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            return process.Id == 0 ? null : process;
        }
        catch (Exception)
        {
            return null;
        }
    }
}

