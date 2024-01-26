using Microsoft.Extensions.DependencyInjection;
using NexusMods.CrossPlatform.Process;
using NexusMods.CrossPlatform.ProtocolRegistration;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform;

/// <summary>
/// Utility class for registering services.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds operating system specific implementations (governed by <see cref="IOSInterop"/>) to the service collection.
    /// </summary>
    /// <param name="services">The services to add OS interop to.</param>
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddCrossPlatform(this IServiceCollection services)
    {
        services
            .AddSingleton(OSInformation.Shared) // Paths
            .AddSingleton<IProcessFactory, ProcessFactory>();

        OSInformation.Shared.SwitchPlatform(
            ref services,
#pragma warning disable CA1416 // macOS
            onWindows: (ref IServiceCollection value) => value.AddSingleton<IProtocolRegistration, ProtocolRegistrationWindows>(),
            onLinux: (ref IServiceCollection value) => value.AddSingleton<IProtocolRegistration, ProtocolRegistrationLinux>(),
            onOSX: (ref IServiceCollection value) => value.AddSingleton<IProtocolRegistration, ProtocolRegistrationOSX>()
#pragma warning restore CA1416
        );

        OSInformation.Shared.SwitchPlatform(
            ref services,
            onWindows: (ref IServiceCollection value) => value.AddSingleton<IOSInterop, OSInteropWindows>(),
            onLinux: (ref IServiceCollection value) => value.AddSingleton<IOSInterop, OSInteropLinux>(),
            onOSX: (ref IServiceCollection value) => value.AddSingleton<IOSInterop, OSInteropOSX>()
        );

        return services;
    }
}
