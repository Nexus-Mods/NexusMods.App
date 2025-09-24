using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Sdk.Settings;

namespace NexusMods.Collections;

public static class Services
{
    public static IServiceCollection AddNexusModsCollections(this IServiceCollection services)
    {
        return services
            .AddManagedCollectionLoadoutGroupModel()
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
