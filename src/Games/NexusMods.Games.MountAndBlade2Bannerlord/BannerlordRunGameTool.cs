using Bannerlord.LauncherManager.External;
using Bannerlord.LauncherManager.Models;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;
namespace NexusMods.Games.MountAndBlade2Bannerlord;

/// <summary>
/// This is to run the game or SMAPI using the shell, which allows them to start their own console,
/// allowing users to interact with it.
/// </summary>
public class BannerlordRunGameTool : RunGameTool<Bannerlord>
{
    private readonly LauncherManagerFactory _launcherManagerFactory;
    
    public BannerlordRunGameTool(IServiceProvider serviceProvider, Bannerlord game)
        : base(serviceProvider, game)
    {
        _launcherManagerFactory = serviceProvider.GetRequiredService<LauncherManagerFactory>();
    }

    protected override bool UseShell { get; set; } = false;
    
    public override Task Execute(Loadout.ReadOnly loadout, CancellationToken cancellationToken, string[]? commandLineArgs)
    {
        commandLineArgs ??= [];
        
        // We need to 'inject' the current set of enabled modules in addition to any existing parameters.
        // This way, external arguments specified by outside entities are preserved.
        var launcherManager = _launcherManagerFactory.Get(loadout.Installation);
        
        // Set the (automatic) load order.
        launcherManager.Sort();
        ILoadOrderStateProvider loadOrderStateProvider = launcherManager;
        var lo = new LoadOrder(loadOrderStateProvider.GetModuleViewModels()!);
        launcherManager.SetGameParameterLoadOrder(lo);
        
        // Add the new arguments
        commandLineArgs = commandLineArgs.Concat(launcherManager.ExecutableParameters).ToArray();

        return base.Execute(loadout, cancellationToken, commandLineArgs);
    }
}
