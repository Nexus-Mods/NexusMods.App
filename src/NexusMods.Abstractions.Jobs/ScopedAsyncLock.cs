using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// A scoped async lock that can be used to lock a resource asynchronously or synchronously. And the lock
/// can be released by disposing the returned releaser.
/// </summary>
public class ScopedAsyncLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Lock the lock, returning a releaser that will release the lock when disposed
    /// </summary>
    /// <returns></returns>
    [MustDisposeResource]
    public async Task<LockReleaser> LockAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        return new LockReleaser(this);
    }

    /// <summary>
    /// Sync version of <see cref="LockAsync"/>
    /// </summary>
    /// <returns></returns>
    [MustDisposeResource]
    public LockReleaser Lock()
    {
        _semaphore.Wait();
        return new LockReleaser(this);
    }


    public struct LockReleaser(ScopedAsyncLock scopedAsyncLock) : IDisposable
    {
        public void Dispose()
        {
            scopedAsyncLock._semaphore.Release();
        }
    }
}
