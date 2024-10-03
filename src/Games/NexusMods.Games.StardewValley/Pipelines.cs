using System.Reactive;
using System.Text;
using BitFaster.Caching.Lru;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Resources.Caching;
using NexusMods.Abstractions.Resources.IO;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Hashing.xxHash64;
using SMAPIManifest = StardewModdingAPI.Toolkit.Serialization.Models.Manifest;

namespace NexusMods.Games.StardewValley;

internal static class Pipelines
{
    public const string ManifestPipelineKey = nameof(ManifestPipelineKey);

    public static IServiceCollection AddPipelines(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddKeyedSingleton<IResourceLoader<SMAPIModLoadoutItem.ReadOnly, SMAPIManifest>>(
            serviceKey: ManifestPipelineKey,
            implementationFactory: static (serviceProvider, _) => CreateManifestPipeline(
                fileStore: serviceProvider.GetRequiredService<IFileStore>()
            )
        );
    }

    public static IResourceLoader<SMAPIModLoadoutItem.ReadOnly, SMAPIManifest> GetManifestPipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<SMAPIModLoadoutItem.ReadOnly, SMAPIManifest>>(serviceKey: ManifestPipelineKey);
    }

    private static IResourceLoader<SMAPIModLoadoutItem.ReadOnly, SMAPIManifest> CreateManifestPipeline(IFileStore fileStore)
    {
        var pipeline = new FileStoreLoader(fileStore)
            .ThenDo(Unit.Default, static (_, _, resource, _) =>
            {
                var bytes = resource.Data;
                var json = Encoding.UTF8.GetString(bytes);

                var manifest = Interop.SMAPIJsonHelper.Deserialize<SMAPIManifest>(json);
                ArgumentNullException.ThrowIfNull(manifest);

                return ValueTask.FromResult(resource.WithData(manifest));
            })
            .UseCache(
                keyGenerator: static hash => hash,
                keyComparer: EqualityComparer<Hash>.Default,
                capacityPartition: new FavorWarmPartition(totalCapacity: 100)
            )
            .ChangeIdentifier<SMAPIModLoadoutItem.ReadOnly, Hash, SMAPIManifest>(
                static mod => mod.Manifest.AsLoadoutFile().Hash
            );

        return pipeline;
    }
}
