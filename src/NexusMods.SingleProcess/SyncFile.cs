using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
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
        
        _sharedArray = new MultiProcessSharedArray(settings.SyncFile, 2 * 8);
    }


    /// <summary>
    /// Combines the process id and port into a single ulong, this can then be used as a CAS value
    /// to atomically update where the main process is running.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private ulong GetThis(int port)
    {
        Span<byte> buffer = stackalloc byte[8];
        MemoryMarshal.Write(buffer, port);
        MemoryMarshal.Write(buffer[4..], Environment.ProcessId);
        return MemoryMarshal.Read<ulong>(buffer);
    }
    
    /// <summary>
    /// Returns the current process and port that's stored in the sync file
    /// </summary>
    /// <returns></returns>
    public (Process? Process, int Port) GetSyncInfo()
    {
        var val = _sharedArray!.Get(0);
        var pid = (int)(val >> 32);
        var port = (int)(val & 0xFFFFFFFF);

        if (pid == 0)
            return (null, port);

        try
        {
            return (Process.GetProcessById(pid), port);
        }
        catch (Exception)
        {
            return (null, port);
        }
    }

    /// <summary>
    /// True if this process is the main process, and the CLIServer has started
    /// </summary>
    public bool IsMainProcess
    {
        get
        {
            var info = GetSyncInfo();
            return info.Process is not null && info.Process.Id == Environment.ProcessId;
        }
    }
    
    /// <summary>
    /// The port that the main process is listening on
    /// </summary>
    public int Port => GetSyncInfo().Port;
    
    /// <summary>
    /// Flags this process as the main process, returns false if another process is already the main process
    /// </summary>
    public bool TrySetMain(int port)
    {
        var id = GetThis(port);
        var current = _sharedArray.Get(0);
        var pid = (int)(current >> 32);
        var process = GetProcessById(pid);
        if (process is not null && process.Id != Environment.ProcessId)
            return false;
        
        return _sharedArray.CompareAndSwap(0, current, id);
    }
    
    /// <summary>
    /// Returns the process with the given id, or null if it doesn't exist
    /// </summary>
    private Process? GetProcessById(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            return process.Id == 0 ? null : process;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
    
}
