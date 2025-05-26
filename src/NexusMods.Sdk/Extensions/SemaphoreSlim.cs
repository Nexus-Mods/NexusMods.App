using JetBrains.Annotations;

namespace NexusMods.Sdk;

/// <summary>
/// Extension methods for <see cref="SemaphoreSlim"/>.
/// </summary>
[PublicAPI]
public static class SemaphoreSlimExtensions
{
    /// <summary>
    /// Wait using <see cref="DisposableSemaphoreSlim"/>.
    /// </summary>
    public static DisposableSemaphoreSlim WaitDisposable(
        this SemaphoreSlim semaphoreSlim,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var entered = semaphoreSlim.Wait(timeout, cancellationToken);
        return new DisposableSemaphoreSlim(semaphoreSlim, entered);
    }

    /// <summary>
    /// Wait infinitely using <see cref="DisposableSemaphoreSlim"/>.
    /// </summary>
    public static DisposableSemaphoreSlim WaitDisposable(
        this SemaphoreSlim semaphoreSlim,
        CancellationToken cancellationToken = default)
    {
        semaphoreSlim.Wait(cancellationToken);
        return new DisposableSemaphoreSlim(semaphoreSlim, entered: true);
    }
}
