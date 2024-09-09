using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.NexusModsLibrary;

namespace NexusMods.Collections;

public static class Services
{
    public static IServiceCollection AddNexusModsCollections(this IServiceCollection services)
    {
        return services.AddNexusCollectionLoadoutGroupModel()
            .AddCollectionVerbs();
    }
}
