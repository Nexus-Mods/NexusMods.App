using JetBrains.Annotations;

namespace NexusMods.Common;

/// <summary>
/// Wrapper around <see cref="SemaphoreSlim"/> that releases it afterwards.
/// </summary>
[PublicAPI]
public readonly struct SemaphoreSlimWaiter : IDisposable
{
    private readonly SemaphoreSlim _semaphoreSlim;

    /// <summary>
    /// Whether or not the current thread entered the semaphore and needs to
    /// release it afterwards.
    /// </summary>
    public bool HasEntered { get; }

    internal SemaphoreSlimWaiter(SemaphoreSlim semaphoreSlim, bool entered)
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
    /// Custom wait using <see cref="SemaphoreSlimWaiter"/>.
    /// </summary>
    public static SemaphoreSlimWaiter CustomWait(
        this SemaphoreSlim semaphoreSlim,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var entered = semaphoreSlim.Wait(timeout, cancellationToken);
        return new SemaphoreSlimWaiter(semaphoreSlim, entered);
    }
}
