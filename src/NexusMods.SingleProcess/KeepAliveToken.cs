using System;
using System.Threading.Tasks;

namespace NexusMods.SingleProcess;

/// <summary>
/// Creates a new <see cref="KeepAliveToken"/> instance that will mark the given <see cref="TaskCompletionSource"/>
/// as completed when disposed.
/// </summary>
public class KeepAliveToken : IDisposable
{
    private bool _disposed;
    private readonly TaskCompletionSource _tcs;

    /// <summary>
    /// Creates a new <see cref="KeepAliveToken"/> instance that will mark the given <see cref="TaskCompletionSource"/>
    /// as completed when disposed.
    /// </summary>
    /// <param name="tcs"></param>
    public KeepAliveToken(TaskCompletionSource tcs)
    {
        _tcs = tcs;
    }

    /// <summary>
    /// Marks the underlying <see cref="TaskCompletionSource"/> as completed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _tcs.TrySetResult();
    }
}
