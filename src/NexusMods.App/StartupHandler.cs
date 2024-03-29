using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.Listeners;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.SingleProcess;

namespace NexusMods.App;

public class StartupHandler(ILogger<StartupHandler> logger, IServiceProvider provider) :
    AStartupHandler(logger, provider.GetRequiredService<IFileSystem>())
{
    private bool mainStarted = false;
    private object mainLock = new();

    public override async Task<int> HandleCliCommandAsync(string[] args, IRenderer renderer, CancellationToken token)
    {

        try
        {
            logger.LogDebug("Running command: {Arguments}", string.Join(' ', args));
            var result = await provider.GetRequiredService<CommandLineConfigurator>().RunAsync(args, renderer, token);
            logger.LogDebug("Command result: {Result}", result);
            return result;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error running command");
            return -1;
        }
    }

    public override Task<int> StartUiWindowAsync()
    {
        logger.LogDebug("Starting UI window");
        var tcs = new TaskCompletionSource<int>();
        
        // If this not the first time this method has been called during this app's lifetime, then
        // just enqueue a create window call.
        
        if (!UiNeedsStartup())
        {
            logger.LogDebug("UI already running, opening a new window on the existing instance");
            Dispatcher.UIThread.Invoke(Startup.ShowMainWindow);
            return Task.FromResult(0);
        }
        
        // Otherwise we need to startup Avalonia. This results in lazy loading of avalonia
        MainThreadData.MainThreadActions.Enqueue( () =>
        {
            tcs.SetResult(0);
            if (!MainThreadData.IsStartingThread)
            {
                logger.LogCritical("UI should only start from the main thread");
                return -1;
            }

            provider.GetRequiredService<NxmRpcListener>();
            Startup.Main(provider, []);
            return 0;
        });
        return tcs.Task;
    }

    /// <summary>
    /// Returns true if the main UI of the app should be started.
    /// </summary>
    /// <returns></returns>
    private bool UiNeedsStartup()
    {
        lock (mainLock)
        {
            if (!mainStarted)
            {
                mainStarted = true;
                return true;
            }
            return false;
        }
    }

    public static string MainProcessVerb => "main-process";
    public override string MainProcessArgument => MainProcessVerb;
}
