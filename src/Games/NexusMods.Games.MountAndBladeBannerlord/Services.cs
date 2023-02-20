using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;

namespace NexusMods.Games.MountAndBladeBannerlord;

public static class Services 
{
    public static IServiceCollection AddMountAndBladeBannerlord(this IServiceCollection services)
    {
        services.AddSingleton<IGame, MountAndBladeBannerlord>();
        services.AddSingleton<IModInstaller, MountAndBladeBannerlordModInstaller>();
        services.AddSingleton<LauncherManagerFactory>();
        services.AddSingleton<ITool, RunBannerlordTool>();
        services.AddSingleton<ITool, RunLauncherTool>();
        return services;
    }
}