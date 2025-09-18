using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            .AddGogDepotModel()
            .AddGogManifestModel()
            .AddSteamManifestModel()
            .AddEpicGameStoreBuildModel()
            .AddFileHashesQueriesSql()
            .AddSingleton<IFileHashesService, FileHashesService>()
            .AddSingleton<IHostedService>(s => (IHostedService)s.GetRequiredService<IFileHashesService>())
            .AddSettings<FileHashesServiceSettings>()
            .AddHashRelationModel();
    }
    
}
