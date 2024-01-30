using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
///     Adds games related serialization services.
/// </summary>
public static class Services
{
    /// <summary>
    ///     Adds known Game entity related serialization services.
    /// </summary>
    public static IServiceCollection AddLoadoutAbstractions(this IServiceCollection services)
    {
        services.AddSingleton<ITypeFinder, TypeFinder>();
        services.AddSingleton<JsonConverter, ModFileIdConverter>();
        services.AddSingleton<JsonConverter, LoadoutIdConverter>();
        services.AddSingleton<JsonConverter, ModIdConverter>();
        return services;
    }
}
