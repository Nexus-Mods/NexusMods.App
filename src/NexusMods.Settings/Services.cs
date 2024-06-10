using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;
using NexusMods.Icons;

namespace NexusMods.Settings;

public static class Services
{
    public static IServiceCollection AddSettingsManager(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<ISettingsManager, SettingsManager>()
            .AddSettingsSection(new SettingsSectionSetup
            {
                Id = SectionId.DefaultValue,
                IconFunc = static () => IconValues.Error,
                Name = "No Section",
            });
    }
}
