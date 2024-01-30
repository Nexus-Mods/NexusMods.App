using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Abstractions.Serialization.Json;

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
        services.AddSingleton<JsonConverter, AbstractClassConverterFactory<AModMetadata>>();
        services.AddSingleton<JsonConverter, AbstractClassConverterFactory<ISortRule<Mod, ModId>>>();
        services.AddSingleton<JsonConverter, ModFileIdConverter>();
        services.AddSingleton<JsonConverter, LoadoutIdConverter>();
        services.AddSingleton<JsonConverter, ModIdConverter>();
        return services;
    }
}
