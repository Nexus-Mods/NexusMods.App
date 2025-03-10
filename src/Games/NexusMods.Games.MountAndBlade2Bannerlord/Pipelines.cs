using System.Reactive;
using System.Xml;
using Bannerlord.ModuleManager;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Resources.Caching;
using NexusMods.Abstractions.Resources.IO;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

internal static class Pipelines
{
    public const string ManifestPipelineKey = nameof(ManifestPipelineKey);

    public static IServiceCollection AddPipelines(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddKeyedSingleton<IResourceLoader<BannerlordModuleLoadoutItem.ReadOnly, ModuleInfoExtended>>(
            serviceKey: ManifestPipelineKey,
            implementationFactory: static (serviceProvider, _) => CreateManifestPipeline(
                fileStore: serviceProvider.GetRequiredService<IFileStore>(),
                connection: serviceProvider.GetRequiredService<IConnection>()
            )
        );
    }

    public static IResourceLoader<BannerlordModuleLoadoutItem.ReadOnly, ModuleInfoExtended> GetManifestPipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<BannerlordModuleLoadoutItem.ReadOnly, ModuleInfoExtended>>(serviceKey: ManifestPipelineKey);
    }

    private static IResourceLoader<BannerlordModuleLoadoutItem.ReadOnly, ModuleInfoExtended> CreateManifestPipeline(IFileStore fileStore, IConnection connection)
    {
        var pipeline = new FileStoreStreamLoader(fileStore)
            .ThenDo(Unit.Default, static (_, _, resource, _) =>
            {
                var doc = new XmlDocument();
                doc.Load(resource.Data);
                var data = ModuleInfoExtended.FromXml(doc);
                return ValueTask.FromResult(resource.WithData(data));
            })
            .StoreInMemory<BannerlordModuleLoadoutItem.ReadOnly, Hash, ModuleInfoExtended>(
                selector: static mod => mod.ModuleInfo.AsLoadoutFile().Hash,
                keyComparer: EqualityComparer<Hash>.Default,
                shouldDeleteKey: (tuple, _) => ValueTask.FromResult(!BannerlordModuleLoadoutItem.Load(connection.Db, tuple.Item2.Id).IsValid())
            );

        return pipeline;
    }
}
