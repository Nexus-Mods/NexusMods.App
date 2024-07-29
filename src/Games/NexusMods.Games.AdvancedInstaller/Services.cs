using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Installers;

namespace NexusMods.Games.AdvancedInstaller;

public static class Services
{
    public static IServiceCollection AddAdvancedInstaller(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddKeyedTransient<IModInstaller, AdvancedManualInstaller>("AdvancedManualInstaller");
    }
}
