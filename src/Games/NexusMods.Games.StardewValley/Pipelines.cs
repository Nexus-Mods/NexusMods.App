using System.Reactive;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Resources.Caching;
using NexusMods.Abstractions.Resources.IO;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
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
                fileStore: serviceProvider.GetRequiredService<IFileStore>(),
                connection: serviceProvider.GetRequiredService<IConnection>()
            )
        );
    }

    public static IResourceLoader<SMAPIModLoadoutItem.ReadOnly, SMAPIManifest> GetManifestPipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<SMAPIModLoadoutItem.ReadOnly, SMAPIManifest>>(serviceKey: ManifestPipelineKey);
    }

    private static IResourceLoader<SMAPIModLoadoutItem.ReadOnly, SMAPIManifest> CreateManifestPipeline(IFileStore fileStore, IConnection connection)
    {
        var pipeline = new FileStoreStreamLoader(fileStore)
            .ThenDo(Unit.Default, static (_, _, resource, _) =>
            {
                using var streamReader = new StreamReader(stream: resource.Data, encoding: Encoding.UTF8);
                var json = streamReader.ReadToEnd();

                var manifest = Interop.SMAPIJsonHelper.Deserialize<SMAPIManifest>(json);
                ArgumentNullException.ThrowIfNull(manifest);

                return ValueTask.FromResult(resource.WithData(manifest));
            })
            .StoreInMemory<SMAPIModLoadoutItem.ReadOnly, Hash, SMAPIManifest>(
                selector: static mod => mod.Manifest.AsLoadoutFile().Hash,
                keyComparer: EqualityComparer<Hash>.Default,
                shouldDeleteKey: (tuple, _) => ValueTask.FromResult(!SMAPIModLoadoutItem.Load(connection.Db, tuple.Item2.Id).IsValid())
            );

        return pipeline;
    }
}
