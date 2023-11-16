using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.Listeners;
using NexusMods.App.UI;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.SingleProcess;

namespace NexusMods.App;

public class StartupHandler(ILogger<StartupHandler> logger, IServiceProvider provider) :
    AStartupHandler(logger, provider.GetRequiredService<IFileSystem>())
{
    public override async Task<int> HandleCliCommandAsync(string[] args, IRenderer renderer, CancellationToken token)
    {
        return await provider.GetRequiredService<CommandLineConfigurator>().RunAsync(args, renderer, token);
    }

    public override Task<int> StartUiWindowAsync()
    {
        provider.GetRequiredService<NxmRpcListener>();
        Startup.Main(provider, Array.Empty<string>());
        return Task.FromResult(0);
    }

    public override string MainProcessArgument => "main-process";
}
