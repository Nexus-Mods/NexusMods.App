using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.ModInstallers;

namespace NexusMods.Games.Reshade;

public static class Services
{
    public static IServiceCollection AddReshade(this IServiceCollection services)
    {
        services.AddSingleton<IModInstaller, ReshadePresetInstaller>();
        return services;
    }

}
