using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Installers;

namespace NexusMods.Games.Reshade;

public static class Services
{
    public static IServiceCollection AddReshade(this IServiceCollection services)
    {
        services.AddSingleton<IModInstaller, ReshadePresetInstaller>();
        return services;
    }

}
