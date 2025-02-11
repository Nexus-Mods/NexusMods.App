using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Games.FileHashes;

public static class Services
{
    /// <summary>
    /// Add services related to the file hashes module.
    /// </summary>
    public static IServiceCollection AddFileHashes(this IServiceCollection services)
    {
        return services.AddFileHashesVerbs()
            .AddPathHashRelationModel()
            .AddVersionDefinitionModel()
            .AddGogBuildModel()
            .AddSteamManifestModel()
            .AddSingleton<IFileHashesService, FileHashesService>()
            .AddSettings<FileHashesServiceSettings>()
            .AddHashRelationModel();
    }
    
}
