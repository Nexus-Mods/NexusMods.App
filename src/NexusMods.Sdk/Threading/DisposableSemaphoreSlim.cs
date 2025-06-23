using JetBrains.Annotations;

namespace NexusMods.Sdk.Threading;

/// <summary>
/// Wrapper around <see cref="SemaphoreSlim"/> that releases the semaphore on dispose.
/// </summary>
[PublicAPI]
public readonly struct DisposableSemaphoreSlim : IDisposable
{
    private readonly SemaphoreSlim _semaphoreSlim;

    /// <summary>
    /// Whether the current thread entered the semaphore and needs to
    /// release it on dispose.
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
