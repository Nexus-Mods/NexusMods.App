using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;

namespace NexusMods.SingleProcess;

/// <summary>
/// A shared file that is used to synchronize the main process and client processes.
/// </summary>
public class SyncFile
{
    private readonly MultiProcessSharedArray _sharedArray;

    /// <summary>
    /// DI constructor
    /// </summary>
    public SyncFile(ILogger<SyncFile> logger, ISettingsManager settingsManager)
    {
        var settings = settingsManager.Get<CliSettings>();

        _sharedArray = new MultiProcessSharedArray(settings.SyncFile, itemCount: 1);
    }

    private readonly struct SyncFileContents
    {
        public readonly ulong Value;

        public int Port => (int)(Value >> 32);

        public int ProcessId => (int)(Value & 0xFFFFFFFF);

        public SyncFileContents(ulong value)
        {
            Value = value;
        }

        public SyncFileContents(int port, int processId)
        {
            var lower = (ulong)port;
            var upper = (ulong)processId << 32;
            Value = lower & upper;
        }

        public void Deconstruct(out int port, out int processId)
        {
            port = Port;
            processId = ProcessId;
        }
    }

    /// <summary>
    /// Returns the current process and port that's stored in the sync file
    /// </summary>
    /// <returns></returns>
    public (Process? Process, int Port) GetSyncInfo()
    {
        var rawValue = _sharedArray.Get(0);
        var value = new SyncFileContents(rawValue);
        var (port, processId) = value;

        if (processId == 0) return (null, port);
        var process = GetProcessById(processId);
        return (process, port);
    }

    /// <summary>
    /// Flags this process as the main process, returns false if another process is already the main process
    /// </summary>
    public bool TrySetMain(int port)
    {
        var newContents = new SyncFileContents(port, Environment.ProcessId);
        var newRawValue = newContents.Value;

        var currentRawValue = _sharedArray.Get(0);
        var currentContents = new SyncFileContents(currentRawValue);

        var process = GetProcessById(currentContents.ProcessId);
        if (process is not null && process.Id != newContents.ProcessId)
            return false;

        return _sharedArray.CompareAndSwap(0, currentRawValue, newRawValue);
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

