using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NexusMods.Common
{
    public static class Services
    {
        public static IServiceCollection AddOSInterop(this IServiceCollection services)
        {
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
    }
}
