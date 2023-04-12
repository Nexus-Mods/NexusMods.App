using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public static class Services
{
    public static IServiceCollection AddMountAndBladeBannerlord(this IServiceCollection services)
    {
        services.AddSingleton<MountAndBlade2Bannerlord>();
        services.AddSingleton<IGame, MountAndBlade2Bannerlord>(sp => sp.GetRequiredService<MountAndBlade2Bannerlord>());
        services.AddSingleton<IModInstaller, MountAndBlade2BannerlordModInstaller>();
        services.AddSingleton<IFileAnalyzer, MountAndBlade2BannerlordAnalyzer>();
        services.AddSingleton<LauncherManagerFactory>();
        services.AddSingleton<ITool, RunStandaloneTool>();
        services.AddSingleton<ITool, RunLauncherTool>();
        return services;
    }
}
