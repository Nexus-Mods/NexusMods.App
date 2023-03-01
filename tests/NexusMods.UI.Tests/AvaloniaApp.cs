using Avalonia.Threading;

namespace NexusMods.UI.Tests;

public class AvaloniaApp
{
    private readonly Thread _thread;

    public AvaloniaApp(IServiceProvider provider)
    {
        var tcs = new TaskCompletionSource<SynchronizationContext>();
        _thread = new Thread(() =>
        {
            try
            {
                var app = NexusMods.App.UI.Startup.BuildAvaloniaApp(provider)
                    .SetupWithoutStarting();
                tcs.SetResult(SynchronizationContext.Current);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
            Dispatcher.UIThread.MainLoop(CancellationToken.None);
        })
        {
            IsBackground = true
        };
        _thread.Start();
        SynchronizationContext.SetSynchronizationContext(tcs.Task.Result);
    }
}