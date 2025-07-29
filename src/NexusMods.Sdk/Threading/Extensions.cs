using JetBrains.Annotations;

namespace NexusMods.Sdk.Threading;

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

    /// <summary>
    /// Wait using <see cref="DisposableSemaphoreSlim"/>.
    /// </summary>
    public static async Task<DisposableSemaphoreSlim> WaitAsyncDisposable(
        this SemaphoreSlim semaphoreSlim,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var hasEntered = await semaphoreSlim.WaitAsync(timeout, cancellationToken: cancellationToken);
        return new DisposableSemaphoreSlim(semaphoreSlim, entered: hasEntered);
    }

    /// <summary>
    /// Wait infinitely using <see cref="DisposableSemaphoreSlim"/>.
    /// </summary>
    public static async Task<DisposableSemaphoreSlim> WaitAsyncDisposable(
        this SemaphoreSlim semaphoreSlim,
        CancellationToken cancellationToken = default)
    {
        await semaphoreSlim.WaitAsync(cancellationToken: cancellationToken);
        return new DisposableSemaphoreSlim(semaphoreSlim, entered: true);
    }
}
