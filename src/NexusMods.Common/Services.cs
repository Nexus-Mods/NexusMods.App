using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NexusMods.Common.OSInterop;

namespace NexusMods.Common
{
    public static class Services
    {
        public static IServiceCollection AddCommon(this IServiceCollection services)
        {
            services.AddSingleton<IIDGenerator, IDGenerator>()
                    .AddSingleton<IProcessFactory, ProcessFactory>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                services.AddSingleton<IOSInterop, OSInteropWindows>();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                services.AddSingleton<IOSInterop, OSInteropLinux>();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                services.AddSingleton<IOSInterop, OSInteropOSX>();
            }
            return services;
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
}
