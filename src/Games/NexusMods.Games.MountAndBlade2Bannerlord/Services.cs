using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.MountAndBlade2Bannerlord.Installers;
using NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public static class Services
{
    public static IServiceCollection AddMountAndBlade2Bannerlord(this IServiceCollection services)
    {
        return services
            .AddGame<MountAndBlade2Bannerlord>()

            // Installers
            .AddSingleton<MountAndBlade2BannerlordModInstaller>()

            // Diagnostics

            // Attributes
            .AddBannerlordModuleLoadoutItemModel()
            .AddModuleInfoFileLoadoutFileModel()
   
            // Misc
            .AddSettings<MountAndBlade2BannerlordSettings>()
            .AddSingleton<LauncherManagerFactory>()
            .AddSingleton<FileSystemProvider>()
            .AddSingleton<NotificationProvider>()
            .AddSingleton<DialogProvider>()
            
            // Pipelines
            .AddPipelines();
    }
}
