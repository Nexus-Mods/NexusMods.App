using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexusMods.Backend.FileExtractor;
using NexusMods.Backend.FileExtractor.Extractors;
using NexusMods.Backend.Jobs;
using NexusMods.Backend.OS;
using NexusMods.Backend.Process;
using NexusMods.Backend.RuntimeDependency;
using NexusMods.Backend.Tracking;
using NexusMods.FileExtractor;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.FileExtractor;
using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.Settings;
using NexusMods.Sdk.Tracking;
using NexusMods.UI.Sdk.Icons;
using NexusMods.UI.Sdk.Settings;

namespace NexusMods.Backend;

public static class ServiceExtensions
{
    public static IServiceCollection AddOSInterop(this IServiceCollection serviceCollection, IOSInformation? os = null)
    {
        os ??= OSInformation.Shared;

        serviceCollection = serviceCollection
            .AddSingleton(os)
            .AddSingleton<IProcessRunner, ProcessRunner>();

        serviceCollection = os.MatchPlatform(
            ref serviceCollection,
            onWindows: static (ref IServiceCollection value) => value.AddSingleton<IOSInterop, WindowsInterop>(),
            onLinux: static (ref IServiceCollection value) => value.AddSingleton<IOSInterop, LinuxInterop>(),
            onOSX: static (ref IServiceCollection value) => value.AddSingleton<IOSInterop, MacOSInterop>()
        );

        return serviceCollection;
    }

    public static IServiceCollection AddRuntimeDependencies(this IServiceCollection serviceCollection, IOSInformation? os = null)
    {
        os ??= OSInformation.Shared;

        serviceCollection = serviceCollection.AddHostedService<RuntimeDependencyChecker>();

        if (os.IsLinux)
        {
            serviceCollection = serviceCollection
                .AddAllSingleton<IRuntimeDependency, XdgSettingsDependency>()
                .AddAllSingleton<IRuntimeDependency, UpdateDesktopDatabaseDependency>();
        }

        return serviceCollection;
    }

    public static IServiceCollection AddTracking(this IServiceCollection serviceCollection, TrackingSettings? settings)
    {
        if (settings is null || !settings.EnableTracking) return serviceCollection;

        return serviceCollection
            .AddSingleton<EventTracker>()
            .AddSingleton<IEventTracker, EventTracker>(provider => provider.GetRequiredService<EventTracker>())
            .AddHostedService<EventTracker>(provider => provider.GetRequiredService<EventTracker>());
    }

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
    
    /// <summary>
    /// Adds file extraction related services to the provided DI container.
    /// </summary>
    public static IServiceCollection AddFileExtractors(this IServiceCollection coll)
    {
        coll.AddSettings<FileExtractorSettings>();
        coll.AddFileExtractorVerbs();
        coll.AddSingleton<IFileExtractor, FileExtractor.FileExtractor>();
        coll.AddSingleton<IExtractor, SevenZipExtractor>();
        coll.AddSingleton<IExtractor, ManagedZipExtractor>();
        coll.TryAddSingleton<TemporaryFileManager, TemporaryFileManagerEx>();
        return coll;
    }
    
    public static IServiceCollection AddJobMonitor(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<IJobMonitor, JobMonitor>();
    }
}
