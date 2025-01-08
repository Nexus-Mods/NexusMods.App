using System.Reactive;
using BitFaster.Caching.Lru;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Resources.Caching;
using NexusMods.Abstractions.Resources.IO;
using NexusMods.Games.Larian.BaldursGate3.Utils.PakParsing;
using NexusMods.Hashing.xxHash3;
using Polly;

namespace NexusMods.Games.Larian.BaldursGate3;

public static class Pipelines
{
    public const string MetadataPipelineKey = nameof(MetadataPipelineKey);

    public static IServiceCollection AddPipelines(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddKeyedSingleton<IResourceLoader<Hash, Outcome<LspkPackageFormat.PakMetaData>>>(
            serviceKey: MetadataPipelineKey,
            implementationFactory: static (serviceProvider, _) => CreateMetadataPipeline(
                fileStore: serviceProvider.GetRequiredService<IFileStore>()
            )
        );
    }

    public static IResourceLoader<Hash, Outcome<LspkPackageFormat.PakMetaData>> GetMetadataPipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<Hash, 
            Outcome<LspkPackageFormat.PakMetaData>>>(serviceKey: MetadataPipelineKey);
    }
    
    private static IResourceLoader<Hash, Outcome<LspkPackageFormat.PakMetaData>> CreateMetadataPipeline(IFileStore fileStore)
    {
        // TODO: change pipeline to return C# 9 type unions instead of OneOf
        var pipeline = new FileStoreStreamLoader(fileStore)
            .ThenDo(Unit.Default,
                static (_, _, resource, _) =>
                {
                    try
                    {
                        var metaFileData = PakFileParser.ParsePakMeta(resource.Data);
                        return ValueTask.FromResult(resource.WithData(Outcome.FromResult(metaFileData)));
                    }
                    catch (InvalidDataException e)
                    {
                        return ValueTask.FromResult(resource.WithData(Outcome.FromException<LspkPackageFormat.PakMetaData>(e)));
                    }
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
