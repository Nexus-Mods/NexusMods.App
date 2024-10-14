using System.Reactive;
using System.Text;
using BitFaster.Caching.Lru;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Resources.Caching;
using NexusMods.Abstractions.Resources.IO;
using NexusMods.Games.Larian.BaldursGate3.Utils.LsxXmlParsing;
using NexusMods.Games.Larian.BaldursGate3.Utils.PakParsing;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.Games.Larian.BaldursGate3;

public static class Pipelines
{
    public const string MetadataPipelineKey = nameof(MetadataPipelineKey);
    
    public static IServiceCollection AddPipelines(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddKeyedSingleton<IResourceLoader<Hash, LsxXmlFormat.MetaFileData>>(
            serviceKey: MetadataPipelineKey,
            implementationFactory: static (serviceProvider, _) => CreateMetadataPipeline(
                fileStore: serviceProvider.GetRequiredService<IFileStore>()
            )
        );
    }
    
    public static IResourceLoader<Hash, LsxXmlFormat.MetaFileData> GetMetadataPipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<Hash, LsxXmlFormat.MetaFileData>>(serviceKey: MetadataPipelineKey);
    }
    
    private static IResourceLoader<Hash, LsxXmlFormat.MetaFileData> CreateMetadataPipeline(IFileStore fileStore)
    {
        var pipeline = new FileStoreStreamLoader(fileStore)
            .ThenDo(Unit.Default,
                static (_, _, resource, _) =>
                {
                    var metaFileData = PakFileParser.ParsePakMeta(resource.Data);
                    return ValueTask.FromResult(resource.WithData(metaFileData));
                }
            )
            .UseCache(
                keyGenerator: static hash => hash,
                keyComparer: EqualityComparer<Hash>.Default,
                capacityPartition: new FavorWarmPartition(totalCapacity: 300)
            );
            
        return pipeline;
    }
}
