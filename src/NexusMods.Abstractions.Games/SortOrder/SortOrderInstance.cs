using NexusMods.Abstractions.Loadouts;
using R3;

namespace NexusMods.Abstractions.Games;

public class SortOrderInstance
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(30);
    
    public SortOrderId SortOrderId { get; }
    
    public async ValueTask<IDisposable> LockAsync(CancellationToken token = default)
    {
        var hasEntered = await _semaphore.WaitAsync(_lockTimeout, token);
        if (hasEntered) return Disposable.Create(() => _semaphore.Release());
        
        // Failed to acquire the lock, check if cancellation was requested
        token.ThrowIfCancellationRequested();
        // Otherwise, throw a timeout exception
        throw new TimeoutException($"Failed to acquire lock after {_lockTimeout.TotalSeconds} seconds.");
    }

    public IDisposable Lock()
    {
        var hasEntered = _semaphore.Wait(_lockTimeout);
        if (hasEntered) return Disposable.Create(() => _semaphore.Release());
        
        // Failed to acquire the lock, throw timeout exception
        throw new TimeoutException($"Failed to acquire lock after {_lockTimeout.TotalSeconds} seconds.");
    }
}
