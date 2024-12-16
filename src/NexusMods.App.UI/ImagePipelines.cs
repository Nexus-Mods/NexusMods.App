using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BitFaster.Caching;
using BitFaster.Caching.Lru;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.IO;
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

namespace NexusMods.App.UI;

public static class ImagePipelines
{
    private const byte ImagePartitionId = 10;
    private const string CollectionTileImagePipelineKey = nameof(CollectionTileImagePipelineKey);
    private const string CollectionBackgroundImagePipelineKey = nameof(CollectionBackgroundImagePipelineKey);
    private const string UserAvatarPipelineKey = nameof(UserAvatarPipelineKey);
    private const string GuidedInstallerRemoteImagePipelineKey = nameof(GuidedInstallerRemoteImagePipelineKey);
    private const string GuidedInstallerFileImagePipelineKey = nameof(GuidedInstallerFileImagePipelineKey);

    private static readonly Bitmap CollectionTileFallback = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/collection-tile-fallback.png")));
    private static readonly Bitmap CollectionBackgroundFallback = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/black-box.png")));
    private static readonly Bitmap UserAvatarFallback = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/avatar.webp")));

    public static Observable<Bitmap> CreateObservable(EntityId input, IResourceLoader<EntityId, Bitmap> pipeline)
    {
        return Observable
            .Return(input)
            .ObserveOnThreadPool()
            .SelectAwait(async (id, cancellationToken) => await pipeline.LoadResourceAsync(id, cancellationToken), configureAwait: false)
            .Select(static resource => resource.Data);
    }

    public static IServiceCollection AddImagePipelines(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddKeyedSingleton<IResourceLoader<EntityId, Bitmap>>(
                serviceKey: UserAvatarPipelineKey,
                implementationFactory: static (serviceProvider, _) => CreateUserAvatarPipeline(
                    httpClient: serviceProvider.GetRequiredService<HttpClient>(),
                    connection: serviceProvider.GetRequiredService<IConnection>()
                )
            )
            .AddKeyedSingleton<IResourceLoader<EntityId, Bitmap>>(
                serviceKey: CollectionTileImagePipelineKey,
                implementationFactory: static (serviceProvider, _) => CreateCollectionTileImagePipeline(
                    httpClient: serviceProvider.GetRequiredService<HttpClient>(),
                    connection: serviceProvider.GetRequiredService<IConnection>()
                )
            )
            .AddKeyedSingleton<IResourceLoader<EntityId, Bitmap>>(
                serviceKey: CollectionBackgroundImagePipelineKey,
                implementationFactory: static (serviceProvider, _) => CreateCollectionBackgroundImagePipeline(
                    httpClient: serviceProvider.GetRequiredService<HttpClient>(),
                    connection: serviceProvider.GetRequiredService<IConnection>()
                )
            )
            .AddKeyedSingleton<IResourceLoader<Uri, Lifetime<Bitmap>>>(
                serviceKey: GuidedInstallerRemoteImagePipelineKey,
                implementationFactory: static (serviceProvider, _) => CreateGuidedInstallerRemoteImagePipeline(
                    httpClient: serviceProvider.GetRequiredService<HttpClient>()
                )
            )
            .AddKeyedSingleton<IResourceLoader<Hash, Lifetime<Bitmap>>>(
                serviceKey: GuidedInstallerFileImagePipelineKey,
                implementationFactory: static (serviceProvider, _) => CreateGuidedInstallerFileImagePipeline(
                    fileStore: serviceProvider.GetRequiredService<IFileStore>()
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

    public static IResourceLoader<Uri, Lifetime<Bitmap>> GetGuidedInstallerRemoteImagePipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<Uri, Lifetime<Bitmap>>>(serviceKey: GuidedInstallerRemoteImagePipelineKey);
    }

    public static IResourceLoader<Hash, Lifetime<Bitmap>> GetGuidedInstallerFileImagePipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<Hash, Lifetime<Bitmap>>>(serviceKey: GuidedInstallerFileImagePipelineKey);
    }

    private static IResourceLoader<EntityId, Bitmap> CreateUserAvatarPipeline(
        HttpClient httpClient,
        IConnection connection)
    {
        var pipeline = new HttpLoader(httpClient)
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
        HttpClient httpClient,
        IConnection connection)
    {
        var pipeline = new HttpLoader(httpClient)
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
        HttpClient httpClient,
        IConnection connection)
    {
        var pipeline = new HttpLoader(httpClient)
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

    private static IResourceLoader<Uri, Lifetime<Bitmap>> CreateGuidedInstallerRemoteImagePipeline(HttpClient httpClient)
    {
        var pipeline = new HttpLoader(httpClient)
            .Decode(decoderType: DecoderType.Skia)
            .ToAvaloniaBitmap()
            .UseScopedCache(
                keyGenerator: static uri => uri,
                keyComparer: EqualityComparer<Uri>.Default,
                capacityPartition: new FavorWarmPartition(totalCapacity: 10)
            );

        return pipeline;
    }

    private static IResourceLoader<Hash, Lifetime<Bitmap>> CreateGuidedInstallerFileImagePipeline(IFileStore fileStore)
    {
        var pipeline = new FileStoreLoader(fileStore)
            .Decode(decoderType: DecoderType.Skia)
            .ToAvaloniaBitmap()
            .UseScopedCache(
                keyGenerator: static hash => hash,
                keyComparer: EqualityComparer<Hash>.Default,
                capacityPartition: new FavorWarmPartition(totalCapacity: 10)
            );

        return pipeline;
    }
}
