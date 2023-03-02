using Avalonia.Threading;

namespace NexusMods.UI.Tests;

public class AvaloniaApp
{
    public AvaloniaApp(IServiceProvider provider)
    {
        var tcs = new TaskCompletionSource<SynchronizationContext>();
        var thread = new Thread(() =>
        {
            try
            {
                _ = App.UI.Startup.BuildAvaloniaApp(provider)
                    .SetupWithoutStarting();
                tcs.SetResult(SynchronizationContext.Current!);
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

        thread.Start();
        SynchronizationContext.SetSynchronizationContext(tcs.Task.Result);
    }
}
