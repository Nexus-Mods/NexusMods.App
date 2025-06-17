namespace NexusMods.Sdk.Threading;

/// <summary>
/// This is an implementation of a simple <see cref="ReaderWriterLock"/>.
/// It differs from the standard <see cref="ReaderWriterLockSlim"/> in that it allows
/// a different thread than the one that acquired the lock to release it.
///
/// This is useful when needing to run asynchronous operations that require a lock.
/// </summary>
public class AsyncFriendlyReaderWriterLock
{
    private const int WriteLockValue = int.MinValue;
    
    /// <summary>
    /// The _lockState value represents the current state of the lock:
    /// - <see cref="WriteLockValue"/> (<see cref="int.MinValue"/>): A write lock is held (GC is in progress)
    /// - 0: No locks are held
    /// - Positive integer: The number of read locks currently held
    /// 
    /// Locking mechanism:
    /// - Write lock (GC):
    ///   - Acquired by atomically setting <see cref="_lockState"/> to <see cref="WriteLockValue"/> if it's 0
    ///   - Released by setting <see cref="_lockState"/> back to 0
    /// - Read lock (File Store):
    ///   - Acquired by atomically incrementing <see cref="_lockState"/> if it's not WriteLockValue
    ///   - Released by decrementing <see cref="_lockState"/>
    /// 
    /// This allows for multiple concurrent readers but only 1 writer.
    /// </summary>
    private int _lockState;

    /// <summary>
    /// Locks the current instance for writing, preventing any other locks from being acquired.
    /// </summary>
    public WriteLockDisposable WriteLock()
    {
        while (true)
        {
            if (Interlocked.CompareExchange(ref _lockState, WriteLockValue, 0) == 0)
                return new WriteLockDisposable(this);

            Thread.Sleep(1);
        }
    }

    /// <summary>
    /// Locks the current instance for reading, allowing multiple concurrent readers.
    /// </summary>
    public ReadLockDisposable ReadLock()
    {
        while (true)
        {
            var current = _lockState;
            if (current != WriteLockValue)
            {
                if (Interlocked.CompareExchange(ref _lockState, current + 1, current) == current)
                    return new ReadLockDisposable(this);
                
                // If code hits here, we've had concurrent increment attempts, so we'll try
                // again next loop.
            }
            else
            {
                // If we're on a write lock, it'll probably take a bit, so we can sleep
                Thread.Sleep(1);
            }
        }
    }

    /// <summary>
    /// Represents a write lock. Use me with the 'using' statement.
    /// </summary>
    public readonly struct WriteLockDisposable : IDisposable
    {
        private readonly AsyncFriendlyReaderWriterLock _lock;

        internal WriteLockDisposable(AsyncFriendlyReaderWriterLock @lock) => _lock = @lock;

        /// <inheritdoc />
        public void Dispose() => Interlocked.Exchange(ref _lock._lockState, 0);
    }

    /// <summary>
    /// Represents a read lock. Use me with the 'using' statement.
    /// </summary>
    public readonly struct ReadLockDisposable : IDisposable
    {
        private readonly AsyncFriendlyReaderWriterLock _lock;

        internal ReadLockDisposable(AsyncFriendlyReaderWriterLock @lock) => _lock = @lock;

        /// <inheritdoc />
        public void Dispose() => Interlocked.Decrement(ref _lock._lockState);
    }
}
