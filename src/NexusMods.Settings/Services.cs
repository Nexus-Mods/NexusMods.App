using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

public static class Services
{
    public static IServiceCollection AddSettings(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<ISettingsManager, SettingsManager>();
    }
}
