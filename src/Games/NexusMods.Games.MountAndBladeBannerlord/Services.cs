using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;

namespace NexusMods.Games.MountAndBladeBannerlord;

public static class Services 
{
    public static IServiceCollection AddMountAndBladeBannerlord(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IGame, MountAndBladeBannerlord>();
        serviceCollection.AddSingleton<IModInstaller, MountAndBladeBannerlordModInstaller>();
        serviceCollection.AddSingleton<NexusModsBannerlordLauncherManagerFactory>();
        return serviceCollection;
    }
    
}