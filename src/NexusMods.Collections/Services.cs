using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.Settings;

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
            .AddCollectionVerbs()
            .AddSingleton<CollectionDownloader>()
            .AddSettings<DownloadSettings>();
    }
}
