using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using NexusMods.Common.OSInterop;

#if DEBUG
using System.Text;
#endif

namespace NexusMods.Common;

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
    public static IServiceCollection AddCommon(this IServiceCollection services)
    {
        services
            .AddOSInformation()
            .AddSingleton<IIDGenerator, IDGenerator>()
            .AddSingleton<IProcessFactory, ProcessFactory>();

        OSInformation.Shared.SwitchPlatform(
            ref services,
            onWindows: (ref IServiceCollection value) => value.AddSingleton<IOSInterop, OSInteropWindows>(),
            onLinux: (ref IServiceCollection value) => value.AddSingleton<IOSInterop, OSInteropLinux>(),
            onOSX: (ref IServiceCollection value) => value.AddSingleton<IOSInterop, OSInteropOSX>()
        );

        return services;
    }

    public static IServiceCollection AddOSInformation(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddSingleton(OSInformation.Shared);
    }

    /// <summary>
    /// Runs through the list of registered services and verifies that there
    /// aren't any duplicate registrations. This is a no-op during release builds.
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <returns></returns>
    public static IServiceCollection Validate(
        this IServiceCollection serviceCollection)
    {
#if DEBUG
        var serviceDescriptors = serviceCollection
            .Where(sd => sd.ImplementationType != null)
            .GroupBy(sd => (sd.ServiceType, sd.ImplementationType))
            .Where(g => g.Count() > 1)
            .Select(g => (g.Key.ServiceType, g.Key.ImplementationType, g.Count()))
            .ToList();

        if (serviceDescriptors.Any())
        {
            var sb = new StringBuilder();
            foreach (var error in serviceDescriptors)
            {
                sb.AppendLine($"  Service: {error.ServiceType}, Implementation: {error.ImplementationType}, Count: {error.Item3}");
            }

            throw new InvalidOperationException(
                $"Duplicate service registrations found: \n{sb}");
        }
#endif
        return serviceCollection;
    }
}
