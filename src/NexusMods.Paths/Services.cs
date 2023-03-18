using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Paths;

/// <summary>
/// DI service helpers.
/// </summary>
[PublicAPI]
public static class Services
{
    /// <summary>
    /// Adds the <see cref="IFileSystem"/> implementation as a singleton.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddFileSystem(this IServiceCollection services)
    {
        services.AddSingleton<IFileSystem>(new FileSystem());
        return services;
    }
}
