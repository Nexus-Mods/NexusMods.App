using Microsoft.Extensions.DependencyInjection;
using NexusMods.Sdk;
using NexusMods.Sdk.Settings;
using NexusMods.UI.Sdk.Icons;
using NexusMods.UI.Sdk.Settings;

namespace NexusMods.Backend;

public static class ServiceExtensions
{
    public static IServiceCollection AddSettingsManager(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<ISettingsManager, SettingsManager>()
            .AddStorageBackend<JsonStorageBackend>()
            .AddStorageBackend<MnemonicDBStorageBackend>(isDefault: true)
            .AddSingleton(new SectionDescriptor(
                Id: Sections.General,
                Name: "General",
                IconFunc: () => IconValues.Desktop,
                Priority: ushort.MaxValue
            ))
            .AddSingleton(new SectionDescriptor(
                Id: Sections.Privacy,
                Name: "Privacy",
                IconFunc: () => IconValues.ShieldHalfFull
            ))
            .AddSingleton(new SectionDescriptor(
                Id: Sections.GameSpecific,
                Name: "Game specific",
                IconFunc: () => IconValues.Game,
                Priority: ushort.MinValue + 3
            ))
            .AddSingleton(new SectionDescriptor(
                Id: Sections.Advanced,
                Name: "Advanced",
                IconFunc: () => IconValues.School,
                Priority: ushort.MinValue + 2
            ))
            .AddSingleton(new SectionDescriptor(
                Id: Sections.DeveloperTools,
                Name: "Developer tools",
                IconFunc: () => IconValues.Code,
                Priority: ushort.MinValue + 1
            ))
            .AddSingleton(new SectionDescriptor(
                Id: Sections.Experimental,
                Name: "Experimental - Not currently supported",
                IconFunc: () => IconValues.WarningAmber,
                Priority: ushort.MinValue,
                Hidden: !ApplicationConstants.IsDebug
            ))
            .AddSettingModel();
    }
}
