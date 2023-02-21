using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public static class Services 
{
    public static IServiceCollection AddMountAndBladeBannerlord(this IServiceCollection services)
    {
        services.AddSingleton<IGame, MountAndBlade2Bannerlord>();
        services.AddSingleton<IModInstaller, MountAndBlade2BannerlordModInstaller>();
        services.AddSingleton<LauncherManagerFactory>();
        services.AddSingleton<ITool, RunBannerlordTool>();
        services.AddSingleton<ITool, RunLauncherTool>();
        return services;
    }
}