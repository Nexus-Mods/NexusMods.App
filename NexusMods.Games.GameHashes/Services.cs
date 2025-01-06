using Microsoft.Extensions.DependencyInjection;
using NexusMods.Games.GameHashes.Models;

namespace NexusMods.Games.GameHashes;

public static class Services
{
    public static IServiceCollection AddGameHashes(this IServiceCollection services)
    {
        services.AddGameHashesVerbs();
        services.AddHashRelationModel();
        services.AddGOGBuildEntryModel();
        services.AddSteamManifestEntryModel();
        return services;
    }
}
