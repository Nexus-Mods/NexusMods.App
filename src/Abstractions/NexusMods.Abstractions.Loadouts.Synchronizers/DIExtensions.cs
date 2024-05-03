using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// DI related extensions for the loadouts abstraction
/// </summary>
public static class DIExtensions
{
    /// <summary>
    /// Register a file generator with the DI container
    /// </summary>
    public static IServiceCollection AddGeneratedFile<TType>(this IServiceCollection services)
        where TType : class, IFileGenerator
    {
        services.AddKeyedSingleton<IFileGenerator, TType>(TType.Guid, (provider, o) => provider.GetRequiredService<TType>());
        services.AddSingleton<TType>();
        return services;
    }
    
}
