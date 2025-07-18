using System.Reactive;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Sdk.Resources;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.FileStore;
using SMAPIManifest = StardewModdingAPI.Toolkit.Serialization.Models.Manifest;

namespace NexusMods.Games.StardewValley;

internal static class Pipelines
{
    public const string ManifestPipelineKey = nameof(ManifestPipelineKey);

    public static IServiceCollection AddPipelines(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddKeyedSingleton<IResourceLoader<SMAPIManifestLoadoutFile.ReadOnly, SMAPIManifest>>(
            serviceKey: ManifestPipelineKey,
            implementationFactory: static (serviceProvider, _) => CreateManifestPipeline(
                fileStore: serviceProvider.GetRequiredService<IFileStore>(),
                connection: serviceProvider.GetRequiredService<IConnection>()
            )
        );
    }

    public static IResourceLoader<SMAPIManifestLoadoutFile.ReadOnly, SMAPIManifest> GetManifestPipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<SMAPIManifestLoadoutFile.ReadOnly, SMAPIManifest>>(serviceKey: ManifestPipelineKey);
    }

    private static IResourceLoader<SMAPIManifestLoadoutFile.ReadOnly, SMAPIManifest> CreateManifestPipeline(IFileStore fileStore, IConnection connection)
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
            .StoreInMemory<SMAPIManifestLoadoutFile.ReadOnly, Hash, SMAPIManifest>(
                selector: static manifest => manifest.AsLoadoutFile().Hash,
                keyComparer: EqualityComparer<Hash>.Default,
                shouldDeleteKey: (tuple, _) => ValueTask.FromResult(!SMAPIManifestLoadoutFile.Load(connection.Db, tuple.Item2.Id).IsValid())
            );

        return pipeline;
    }
}
