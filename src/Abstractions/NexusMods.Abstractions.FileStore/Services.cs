using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Abstractions.Serialization.Json;

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
        services.AddSingleton<JsonConverter, AbstractClassConverterFactory<AArchiveMetaData>>();
        services.AddSingleton<ITypeFinder, TypeFinder>();
        return services;
    }
}
