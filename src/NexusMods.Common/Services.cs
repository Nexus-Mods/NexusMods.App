using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;

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
        services.AddSingleton<IIDGenerator, IDGenerator>()
            .AddSingleton<IProcessFactory, ProcessFactory>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            services.AddSingleton<IOSInterop, OSInteropWindows>();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            services.AddSingleton<IOSInterop, OSInteropLinux>();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            services.AddSingleton<IOSInterop, OSInteropOSX>();

        return services;
    }
}
