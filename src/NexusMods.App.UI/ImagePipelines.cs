using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Resources.DB;
using NexusMods.Abstractions.Resources.IO;
using NexusMods.Abstractions.Resources.Resilience;
using NexusMods.Hashing.xxHash64;
using NexusMods.Media;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI;

internal static class ImagePipelines
{
    private const byte ImagePartitionId = 10;
    private const string CollectionTileImagePipelineKey = nameof(CollectionTileImagePipelineKey);
    private const string CollectionBackgroundImagePipelineKey = nameof(CollectionBackgroundImagePipelineKey);

    private static readonly Bitmap CollectionTileFallback = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/collection-tile-fallback.png")));

    public static IServiceCollection AddImagePipelines(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddKeyedSingleton<IResourceLoader<EntityId, Bitmap>>(
                serviceKey: CollectionTileImagePipelineKey,
                implementationFactory: static (serviceProvider, _) => CreateCollectionTileImagePipeline(
                    connection: serviceProvider.GetRequiredService<IConnection>()
                )
            )
            .AddKeyedSingleton<IResourceLoader<EntityId, Bitmap>>(
                serviceKey: CollectionBackgroundImagePipelineKey,
                implementationFactory: static (serviceProvider, _) => CreateCollectionBackgroundImagePipeline(
                    connection: serviceProvider.GetRequiredService<IConnection>()
                )
            );
    }

    public static IResourceLoader<EntityId, Bitmap> GetCollectionTileImagePipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<EntityId, Bitmap>>(serviceKey: CollectionTileImagePipelineKey);
    }

    public static IResourceLoader<EntityId, Bitmap> GetCollectionBackgroundImagePipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<EntityId, Bitmap>>(serviceKey: CollectionBackgroundImagePipelineKey);
    }

    private static IResourceLoader<EntityId, Bitmap> CreateCollectionTileImagePipeline(
        IConnection connection)
    {
        var pipeline = new HttpLoader(new HttpClient())
            .ChangeIdentifier<ValueTuple<EntityId, Uri>, Uri, byte[]>(static tuple => tuple.Item2)
            .PersistInDb(
                connection: connection,
                referenceAttribute: CollectionMetadata.TileImageResource,
                identifierToHash: static uri => uri.ToString().XxHash64AsUtf8(),
                partitionId: PartitionId.User(ImagePartitionId)
            )
            .Decode(decoderType: DecoderType.Skia)
            .ToAvaloniaBitmap()
            .UseFallbackValue(CollectionTileFallback)
            .EntityIdToIdentifier(
                connection: connection,
                attribute: CollectionMetadata.TileImageUri
            );

        return pipeline;
    }

    private static IResourceLoader<EntityId, Bitmap> CreateCollectionBackgroundImagePipeline(
        IConnection connection)
    {
        var pipeline = new HttpLoader(new HttpClient())
            .ChangeIdentifier<ValueTuple<EntityId, Uri>, Uri, byte[]>(static tuple => tuple.Item2)
            .PersistInDb(
                connection: connection,
                referenceAttribute: CollectionMetadata.BackgroundImageResource,
                identifierToHash: static uri => uri.ToString().XxHash64AsUtf8(),
                partitionId: PartitionId.User(ImagePartitionId)
            )
            .Decode(decoderType: DecoderType.Skia)
            .ToAvaloniaBitmap()
            // TODO: .UseFallbackValue()
            .EntityIdToIdentifier(
                connection: connection,
                attribute: CollectionMetadata.BackgroundImageUri
            );

        return pipeline;
    }
}
