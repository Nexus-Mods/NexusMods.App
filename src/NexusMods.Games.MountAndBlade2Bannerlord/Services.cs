using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Sdk.Settings;
using NexusMods.Games.MountAndBlade2Bannerlord.Installers;
using NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public static class Services
{
    public static IServiceCollection AddMountAndBlade2Bannerlord(this IServiceCollection services)
    {
        return services
            .AddGame<Bannerlord>()
            .AddSingleton<ITool, BannerlordRunGameTool>()

            // Installers
            .AddSingleton<BLSEInstaller>()
            .AddSingleton<BannerlordModInstaller>()

            // Diagnostics

            // Attributes
            .AddBannerlordModuleLoadoutItemModel()
            .AddModuleInfoFileLoadoutFileModel()
   
            // Misc
            .AddSettings<BannerlordSettings>()
            .AddSingleton<LauncherManagerFactory>()
            .AddSingleton<FileSystemProvider>()
            .AddSingleton<NotificationProvider>()
            .AddSingleton<DialogProvider>()
            
            // Pipelines
            .AddPipelines();
    }
}
