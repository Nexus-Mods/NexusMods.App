namespace NexusMods.DataModel.Interprocess;

internal readonly struct SemaphoreSlimWaiter : IDisposable
{
    private readonly SemaphoreSlim _semaphoreSlim;

    public bool HasEntered { get; }

    internal SemaphoreSlimWaiter(SemaphoreSlim semaphoreSlim, bool entered)
    {
        _semaphoreSlim = semaphoreSlim;
        HasEntered = entered;
    }

    public void Dispose()
    {
        if (!HasEntered) return;
        _semaphoreSlim.Release();
    }
}

internal static class SemaphoreExtensions
{
    public static SemaphoreSlimWaiter CustomWait(
        this SemaphoreSlim semaphoreSlim,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var entered = semaphoreSlim.Wait(timeout, cancellationToken);
        return new SemaphoreSlimWaiter(semaphoreSlim, entered);
    }
}
