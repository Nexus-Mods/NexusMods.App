using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games.FileHashes.Models;

namespace NexusMods.Abstractions.Games.FileHashes;

public static class Services
{
    /// <summary>
    /// Add services related to the file hashes module.
    /// </summary>
    public static IServiceCollection AddFileHashes(this IServiceCollection services)
    {
        return services.AddFileHashesVerbs()
            .AddPathHashRelationModel()
            .AddGogBuildModel()
            .AddSteamManifestModel()
            .AddHashRelationModel();
    }
    
}
