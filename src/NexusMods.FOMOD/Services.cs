using FomodInstaller.Interface;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.ModInstallers;

namespace NexusMods.FOMOD;

public static class Services
{
    public static IServiceCollection AddFOMOD(this IServiceCollection services)
    {
        services
            .AddSingleton<ICoreDelegates, InstallerDelegates>()
            .AddAllSingleton<IModInstaller, FomodXMLInstaller>()
            ;
        return services;
    }
}
