using System.Reactive;
using BitFaster.Caching.Lru;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Resources.Caching;
using NexusMods.Abstractions.Resources.IO;
using NexusMods.Games.Larian.BaldursGate3.Utils.LsxXmlParsing;
using NexusMods.Games.Larian.BaldursGate3.Utils.PakParsing;
using NexusMods.Hashing.xxHash64;
using OneOf.Types;

namespace NexusMods.Games.Larian.BaldursGate3;

public static class Pipelines
{
    public const string MetadataPipelineKey = nameof(MetadataPipelineKey);

    public static IServiceCollection AddPipelines(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddKeyedSingleton<IResourceLoader<Hash, OneOf.OneOf<LsxXmlFormat.MetaFileData, Error<InvalidDataException>>>>(
            serviceKey: MetadataPipelineKey,
            implementationFactory: static (serviceProvider, _) => CreateMetadataPipeline(
                fileStore: serviceProvider.GetRequiredService<IFileStore>()
            )
        );
    }

    public static IResourceLoader<Hash, OneOf.OneOf<LsxXmlFormat.MetaFileData, Error<InvalidDataException>>> GetMetadataPipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<Hash, 
            OneOf.OneOf<LsxXmlFormat.MetaFileData, Error<InvalidDataException>>>>(serviceKey: MetadataPipelineKey);
    }
    
    private static IResourceLoader<Hash, OneOf.OneOf<LsxXmlFormat.MetaFileData, Error<InvalidDataException>>> CreateMetadataPipeline(IFileStore fileStore)
    {
        // TODO: change pipeline to return C# 9 type unions instead of OneOf
        var pipeline = new FileStoreStreamLoader(fileStore)
            .ThenDo<Hash, OneOf.OneOf<LsxXmlFormat.MetaFileData, Error<InvalidDataException>>, Stream, Unit>(Unit.Default,
                static (_, _, resource, _) =>
                {
                    try
                    {
                        var metaFileData = PakFileParser.ParsePakMeta(resource.Data);
                        return ValueTask.FromResult(resource.WithData(OneOf.OneOf<LsxXmlFormat.MetaFileData, Error<InvalidDataException>>.FromT0(metaFileData)));
                    }
                    catch (InvalidDataException e)
                    {
                        return ValueTask.FromResult(resource.WithData(OneOf.OneOf<LsxXmlFormat.MetaFileData, Error<InvalidDataException>>.FromT1(new Error<InvalidDataException>(e))));
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
