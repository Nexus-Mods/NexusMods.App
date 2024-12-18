using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BitFaster.Caching;
using BitFaster.Caching.Lru;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Resources.Caching;
using NexusMods.Abstractions.Resources.DB;
using NexusMods.Abstractions.Resources.IO;
using NexusMods.Abstractions.Resources.Resilience;
using NexusMods.Hashing.xxHash3;
using NexusMods.Media;
using NexusMods.MnemonicDB.Abstractions;
using R3;
using SkiaSharp;

namespace NexusMods.App.UI;

internal static class ImagePipelines
{
    private const byte ImagePartitionId = 10;
    private const string CollectionTileImagePipelineKey = nameof(CollectionTileImagePipelineKey);
    private const string CollectionBackgroundImagePipelineKey = nameof(CollectionBackgroundImagePipelineKey);
    private const string UserAvatarPipelineKey = nameof(UserAvatarPipelineKey);
    private const string ModPageThumbnailPipelineKey = nameof(ModPageThumbnailPipelineKey);

    private static readonly Bitmap CollectionTileFallback = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/collection-tile-fallback.png")));
    private static readonly Bitmap CollectionBackgroundFallback = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/black-box.png")));
    private static readonly Bitmap UserAvatarFallback = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/avatar.webp")));
    private static readonly Bitmap ModPageThumbnailFallback = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/black-box.png"))); // TODO: Update This

    public static Observable<Bitmap> CreateObservable(EntityId input, IResourceLoader<EntityId, Bitmap> pipeline)
    {
        return Observable
            .Return(input)
            .ObserveOnThreadPool()
            .SelectAwait(async (id, cancellationToken) => await pipeline.LoadResourceAsync(id, cancellationToken), configureAwait: false)
            .Select(static resource => resource.Data);
    }
    
    public static Observable<Bitmap> CreateObservable(EntityId input, IResourceLoader<EntityId, Lifetime<Bitmap>> pipeline)
    {
        return Observable
            .Return(input)
            .ObserveOnThreadPool()
            .SelectAwait(async (id, cancellationToken) => await pipeline.LoadResourceAsync(id, cancellationToken), configureAwait: false)
            .Select(static resource => resource.Data.Value);
    }

    public static IServiceCollection AddImagePipelines(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddKeyedSingleton<IResourceLoader<EntityId, Bitmap>>(
                serviceKey: UserAvatarPipelineKey,
                implementationFactory: static (serviceProvider, _) => CreateUserAvatarPipeline(
                    connection: serviceProvider.GetRequiredService<IConnection>()
                )
            )
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
            )
            .AddKeyedSingleton<IResourceLoader<EntityId, Lifetime<Bitmap>>>(
                serviceKey: ModPageThumbnailPipelineKey,
                implementationFactory: static (serviceProvider, _) => CreateModPageThumbnailPipeline(
                    connection: serviceProvider.GetRequiredService<IConnection>()
                )
            );
    }

    public static IResourceLoader<EntityId, Bitmap> GetUserAvatarPipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<EntityId, Bitmap>>(serviceKey: UserAvatarPipelineKey);
    }

    public static IResourceLoader<EntityId, Bitmap> GetCollectionTileImagePipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<EntityId, Bitmap>>(serviceKey: CollectionTileImagePipelineKey);
    }

    public static IResourceLoader<EntityId, Bitmap> GetCollectionBackgroundImagePipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<EntityId, Bitmap>>(serviceKey: CollectionBackgroundImagePipelineKey);
    }
    
    /// <summary>
    /// Input: ModPageMetadataId
    /// Output: Image (cached)
    /// </summary>
    public static IResourceLoader<EntityId, Lifetime<Bitmap>> GetModPageThumbnailPipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<EntityId, Lifetime<Bitmap>>>(serviceKey: ModPageThumbnailPipelineKey);
    }

    private static IResourceLoader<EntityId, Bitmap> CreateUserAvatarPipeline(IConnection connection)
    {
        var pipeline = new HttpLoader(new HttpClient())
            .ChangeIdentifier<ValueTuple<EntityId, Uri>, Uri, byte[]>(static tuple => tuple.Item2)
            .PersistInDb(
                connection: connection,
                referenceAttribute: User.AvatarResource,
                identifierToHash: static uri => uri.ToString().xxHash3AsUtf8(),
                partitionId: PartitionId.User(ImagePartitionId)
            )
            .Decode(decoderType: DecoderType.Skia)
            .ToAvaloniaBitmap()
            .UseFallbackValue(UserAvatarFallback)
            .EntityIdToIdentifier(
                connection: connection,
                attribute: User.AvatarUri
            );

        return pipeline;
    }

    private static IResourceLoader<EntityId, Bitmap> CreateCollectionTileImagePipeline(
        IConnection connection)
    {
        var pipeline = new HttpLoader(new HttpClient())
            .ChangeIdentifier<ValueTuple<EntityId, Uri>, Uri, byte[]>(static tuple => tuple.Item2)
            .PersistInDb(
                connection: connection,
                referenceAttribute: CollectionMetadata.TileImageResource,
                identifierToHash: static uri => uri.ToString().xxHash3AsUtf8(),
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
                identifierToHash: static uri => uri.ToString().xxHash3AsUtf8(),
                partitionId: PartitionId.User(ImagePartitionId)
            )
            .Decode(decoderType: DecoderType.Skia)
            .ToAvaloniaBitmap()
            .UseFallbackValue(CollectionBackgroundFallback)
            .EntityIdToIdentifier(
                connection: connection,
                attribute: CollectionMetadata.BackgroundImageUri
            );

        return pipeline;
    }
    
    /// <summary>
    /// Input: ModPageMetadataId
    /// Output: Image (cached)
    /// </summary>
    private static IResourceLoader<EntityId, Lifetime<Bitmap>> CreateModPageThumbnailPipeline(
        IConnection connection)
    {
        var pipeline = new HttpLoader(new HttpClient())
            .ChangeIdentifier<ValueTuple<EntityId, Uri>, Uri, byte[]>(static tuple => tuple.Item2)
            .Decode(decoderType: DecoderType.Skia)
            .Resize(newSize: new SKSizeI(
                width: 120, // TODO: Replace with lausandy dimensions
                height: 80
            ))
            .Encode(encoderType: EncoderType.Qoi)
            .PersistInDb(
                connection: connection,
                referenceAttribute: NexusModsModPageMetadata.ThumbnailResource,
                identifierToHash: static uri => uri.ToString().xxHash3AsUtf8(),
                partitionId: PartitionId.User(ImagePartitionId)
            )
            .Decode(decoderType: DecoderType.Qoi)
            .ToAvaloniaBitmap()
            .UseFallbackValue(ModPageThumbnailFallback)
            .UseScopedCache(
                keyGenerator: static tuple => tuple.Item1,
                keyComparer: EqualityComparer<EntityId>.Default,
                capacityPartition: new FavorWarmPartition(totalCapacity: 333, warmRatio: FavorWarmPartition.DefaultWarmRatio)
            )
            .EntityIdToIdentifier(
                connection: connection,
                attribute: NexusModsModPageMetadata.ThumbnailUri
            );

        return pipeline;
    }
}
