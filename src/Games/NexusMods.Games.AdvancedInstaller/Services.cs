using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Games.AdvancedInstaller;

public static class Services
{
    public static IServiceCollection AddAdvancedInstaller(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddKeyedTransient<IModInstaller, AdvancedManualInstaller>("AdvancedManualInstaller");
    }
}
