using LinuxDesktopUtils.XDGDesktopPortal;

namespace NexusMods.CrossPlatform;

internal sealed class DesktopPortalConnectionManagerWrapper : IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphoreSlim = new(initialCount: 1, maxCount: 1);
    private DesktopPortalConnectionManager? _instance;

    private async ValueTask<DesktopPortalConnectionManager> InitAsync()
    {
        await _semaphoreSlim.WaitAsync(timeout: TimeSpan.FromSeconds(10));

        var manager = await DesktopPortalConnectionManager.ConnectAsync();
        _instance = manager;

        return manager;
    }

    public async ValueTask<DesktopPortalConnectionManager> GetInstance()
    {
        if (_instance is not null) return _instance;
        return await InitAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_instance is not null) await _instance.DisposeAsync();
    }
}
