using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.ModInstallers;

namespace NexusMods.Games.AdvancedInstaller;

public static class Services
{
    public static IServiceCollection AddAdvancedInstaller(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddTransient<IModInstaller, AdvancedManualInstaller>();
    }
}
