using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

public static class Services
{
    public static IServiceCollection AddSettingsManager(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<ISettingsManager, SettingsManager>();
    }
}
