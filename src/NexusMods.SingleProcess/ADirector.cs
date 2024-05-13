using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NexusMods.SingleProcess;

/// <summary>
/// Base class for directors, gathers some duplicateded logic
/// </summary>
/// <param name="settings"></param>
public abstract class ADirector(SingleProcessSettings settings) : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// The single process settings
    /// </summary>
    protected readonly SingleProcessSettings Settings = settings;

    /// <summary>
    /// The shared sync array;
    /// </summary>
    protected ISharedArray? SharedArray;

    /// <inheritdoc />
    public abstract ValueTask DisposeAsync();


    /// <summary>
    /// Connect to the shared array
    /// </summary>
    protected void ConnectSharedArray()
    {
        SharedArray = new MultiProcessSharedArray(Settings.SyncFile, (int)(Settings.SyncFileSize.Value / 8));
    }

    /// <summary>
    /// Returns true if this process is the main process
    /// </summary>
    public bool IsMainProcess => SharedArray is not null && GetSyncInfo().Process is not null;

    /// <summary>
    /// Returns the current process and port that's stored in the sync file
    /// </summary>
    /// <returns></returns>
    protected (Process? Process, int Port) GetSyncInfo()
    {
        var val = SharedArray!.Get(0);
        var pid = (int)(val >> 32);
        var port = (int)(val & 0xFFFFFFFF);

        if (pid == 0)
            return (null, port);

        try
        {
            return (Process.GetProcessById(pid), port);
        }
        catch (ArgumentException)
        {
            return (null, port);
        }
    }

    /// <summary>
    /// Sync version of the dispose method
    /// </summary>
    public void Dispose()
    {
        if (this is IAsyncDisposable ad)
            ad.DisposeAsync()
                .AsTask()
                .Wait(CancellationToken.None);
    }
}
