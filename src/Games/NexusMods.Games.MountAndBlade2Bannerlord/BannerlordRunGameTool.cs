using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
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
}
