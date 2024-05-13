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
    private static bool mainStarted = false;
    private static object mainLock = new();

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
    
    /// <summary>
    /// Returns true if the main UI of the app should be started.
    /// </summary>
    /// <returns></returns>
    private static bool UiNeedsStartup()
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
}
