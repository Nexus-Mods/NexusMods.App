using System.Reactive;
using System.Xml;
using Bannerlord.ModuleManager;
using BitFaster.Caching.Lru;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Resources.Caching;
using NexusMods.Abstractions.Resources.IO;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
using NexusMods.Hashing.xxHash3;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

internal static class Pipelines
{
    public const string ManifestPipelineKey = nameof(ManifestPipelineKey);

    public static IServiceCollection AddPipelines(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddKeyedSingleton<IResourceLoader<BannerlordModuleLoadoutItem.ReadOnly, ModuleInfoExtended>>(
            serviceKey: ManifestPipelineKey,
            implementationFactory: static (serviceProvider, _) => CreateManifestPipeline(
                fileStore: serviceProvider.GetRequiredService<IFileStore>()
            )
        );
    }

    public static IResourceLoader<BannerlordModuleLoadoutItem.ReadOnly, ModuleInfoExtended> GetManifestPipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<BannerlordModuleLoadoutItem.ReadOnly, ModuleInfoExtended>>(serviceKey: ManifestPipelineKey);
    }

    private static IResourceLoader<BannerlordModuleLoadoutItem.ReadOnly, ModuleInfoExtended> CreateManifestPipeline(IFileStore fileStore)
    {
        var pipeline = new FileStoreStreamLoader(fileStore)
            .ThenDo(Unit.Default, static (_, _, resource, _) =>
            {
                var doc = new XmlDocument();
                doc.Load(resource.Data);
                var data = ModuleInfoExtended.FromXml(doc);
                return ValueTask.FromResult(resource.WithData(data));
            })
            .UseCache(
                keyGenerator: static hash => hash,
                keyComparer: EqualityComparer<Hash>.Default,
                capacityPartition: new FavorWarmPartition(totalCapacity: 1000)
            )
            .ChangeIdentifier<BannerlordModuleLoadoutItem.ReadOnly, Hash, ModuleInfoExtended>(
                static mod => mod.ModuleInfo.AsLoadoutFile().Hash
            );

        return pipeline;
    }
}
