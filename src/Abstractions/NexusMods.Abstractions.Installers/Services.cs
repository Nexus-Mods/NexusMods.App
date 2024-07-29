using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Extensions.DependencyInjection;

namespace NexusMods.Abstractions.Installers;

/// <summary>
///     Adds games related serialization services.
/// </summary>
public static class Services
{
    /// <summary>
    ///     Adds known Installer related entity serialization services.
    /// </summary>
    public static IServiceCollection AddInstallerTypes(this IServiceCollection services)
    {
        services.AddAllSingleton<ITypeFinder, TypeFinder>();
        return services;
    }
}
