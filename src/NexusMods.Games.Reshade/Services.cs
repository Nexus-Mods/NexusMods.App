using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Games.Reshade;

public static class Services
{
    public static IServiceCollection AddReshade(this IServiceCollection services)
    {
        services.AddSingleton<ReshadePresetInstaller>();
        return services;
    }

}
