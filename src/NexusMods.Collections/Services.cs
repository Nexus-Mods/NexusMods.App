using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;

namespace NexusMods.Collections;

public static class Services
{
    public static IServiceCollection AddNexusModsCollections(this IServiceCollection services)
    {
        return services
            .AddNexusCollectionLoadoutGroupModel()
            .AddDirectDownloadLibraryFileModel()
            .AddNexusCollectionBundledLoadoutGroupModel()
            .AddNexusCollectionItemLoadoutGroupModel()
            .AddNexusCollectionReplicatedLoadoutGroupModel()
            .AddCollectionVerbs();
    }
}
