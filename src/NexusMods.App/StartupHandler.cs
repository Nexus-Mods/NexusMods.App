using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.Listeners;
using NexusMods.App.UI.Windows;
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
        MainThreadData.MainThreadActions.Enqueue( () =>
        {
            try
            {
                if (!MainThreadData.IsStartingThread)
                {
                    logger.LogCritical("UI should only start from the main thread");
                    return -1;
                }

                if (!StartUI())
                {
                    logger.LogDebug("UI already running, opening a new window on the existing instance");
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        var reactiveWindow = provider.GetRequiredService<MainWindow>();
                        reactiveWindow.ViewModel = provider.GetRequiredService<MainWindowViewModel>();
                        reactiveWindow.Show();
                    });
                    logger.LogDebug("UI window opened");
                }
                else
                {
                    provider.GetRequiredService<NxmRpcListener>();
                    Startup.Main(provider, []);
                }
            }
            finally
            {
                tcs.SetResult(0);
            }
            return 0;
        });
        return tcs.Task;
    }

    /// <summary>
    /// Returns true if the main UI of the app should be started.
    /// </summary>
    /// <returns></returns>
    private bool StartUI()
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
