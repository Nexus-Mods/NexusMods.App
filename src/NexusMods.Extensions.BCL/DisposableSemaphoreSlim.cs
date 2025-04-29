using JetBrains.Annotations;

namespace NexusMods.Extensions.BCL;

/// <summary>
/// Wrapper around <see cref="SemaphoreSlim"/> that releases it afterwards.
/// </summary>
[PublicAPI]
public readonly struct DisposableSemaphoreSlim : IDisposable
{
    private readonly SemaphoreSlim _semaphoreSlim;

    /// <summary>
    /// Whether or not the current thread entered the semaphore and needs to
    /// release it afterwards.
    /// </summary>
    public bool HasEntered { get; }

    internal DisposableSemaphoreSlim(SemaphoreSlim semaphoreSlim, bool entered)
    {
        _semaphoreSlim = semaphoreSlim;
        HasEntered = entered;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!HasEntered) return;
        _semaphoreSlim.Release();
    }
}

/// <summary>
/// Extension methods for <see cref="SemaphoreSlim"/>.
/// </summary>
[PublicAPI]
public static class SemaphoreExtensions
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
