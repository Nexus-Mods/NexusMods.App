using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;
using NexusMods.Sdk;
using NexusMods.UI.Sdk.Icons;

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
                Priority = ushort.MaxValue,
            })
            .AddSettingsSection(new SettingsSectionSetup
            {
                Id = Sections.Privacy,
                IconFunc = static () => IconValues.ShieldHalfFull,
                Name = "Privacy",
            })
            .AddSettingsSection(new SettingsSectionSetup
            {
                Id = Sections.Advanced,
                IconFunc = static () => IconValues.School,
                Name = "Advanced",
                Priority = ushort.MinValue + 2,
            })
            .AddSettingsSection(new SettingsSectionSetup
            {
                Id = Sections.DeveloperTools,
                IconFunc = static () => IconValues.Code,
                Name = "Developer tools",
                Priority = ushort.MinValue + 1,
            })
            .AddSettingsSection(new SettingsSectionSetup
            {
                Id = Sections.Experimental,
                IconFunc = static () => IconValues.WarningAmber,
                Name = "Experimental - Not currently supported",
                Priority = ushort.MinValue,
                Hidden = !ApplicationConstants.IsDebug,
            })
            .AddSettingsSection(new SettingsSectionSetup
            {
                Id = Sections.GameSpecific,
                IconFunc = static () => IconValues.Game,
                Name = "Game specific",
                Priority = ushort.MinValue + 3,
            });
    }
}
