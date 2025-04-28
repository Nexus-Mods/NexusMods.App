using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Abstractions.Serialization.Json;

namespace NexusMods.Abstractions.DataModel.Entities;

/// <summary>
///     Adds games related serialization services.
/// </summary>
public static class Services
{
    /// <summary>
    ///     Adds known DataModel entity related serialization services.
    /// </summary>
    public static IServiceCollection AddDataModelEntities(this IServiceCollection services)
    {
        services.AddSingleton<JsonConverter, AbstractClassConverterFactory<AModMetadata>>();
        services.AddSingleton<JsonConverter, AbstractClassConverterFactory<ISortRule<Mod, ModId>>>();
        services.AddSingleton<ITypeFinder, TypeFinder>();
        return services;
    }
}
