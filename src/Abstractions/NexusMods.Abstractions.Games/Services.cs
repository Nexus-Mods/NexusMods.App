using System.Text.Json.Serialization;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games.ArchiveMetadata;
using NexusMods.Abstractions.Games.Json;
using NexusMods.Abstractions.Serialization.Json;
using NexusMods.Extensions.DependencyInjection;

namespace NexusMods.Abstractions.Games;

/// <summary>
///     Adds games related serialization services.
/// </summary>
public static class Services
{
    /// <summary>
    ///     Adds known Game entity related serialization services.
    /// </summary>
    public static IServiceCollection AddGames(this IServiceCollection services)
    {
        services.AddSingleton<JsonConverter, AbstractClassConverterFactory<AArchiveMetaData>>();
        services.AddSingleton<JsonConverter, GameInstallationConverter>();
        services.AddAllSingleton<ITypeFinder, TypeFinder>();
        return services;
    }
}
