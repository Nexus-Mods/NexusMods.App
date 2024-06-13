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
                Id = Sections.General,
                IconFunc = static () => IconValues.Desktop,
                Name = "General",
            })
            .AddSettingsSection(new SettingsSectionSetup
            {
                Id = Sections.Privacy,
                IconFunc = static () => IconValues.BarChart,
                Name = "Privacy",
            })
            .AddSettingsSection(new SettingsSectionSetup
            {
                Id = Sections.Advanced,
                IconFunc = static () => IconValues.School,
                Name = "Advanced",
            })
            .AddSettingsSection(new SettingsSectionSetup
            {
                Id = Sections.DeveloperTools,
                IconFunc = static () => IconValues.Code,
                Name = "Developer tools",
            })
            .AddSettingsSection(new SettingsSectionSetup
            {
                Id = Sections.Experimental,
                IconFunc = static () => IconValues.WarningAmber,
                Name = "Experimental - Not currently supported",
            });
    }
}
