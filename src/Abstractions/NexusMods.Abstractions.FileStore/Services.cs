using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;

namespace NexusMods.Abstractions.FileStore;

/// <summary>
///     Adds games related serialization services.
/// </summary>
public static class Services
{
    /// <summary>
    ///     Adds known Game entity related serialization services.
    /// </summary>
    public static IServiceCollection AddFileStoreAbstractions(this IServiceCollection services)
    {
        services.AddSingleton<ITypeFinder, TypeFinder>();
        return services;
    }
}
