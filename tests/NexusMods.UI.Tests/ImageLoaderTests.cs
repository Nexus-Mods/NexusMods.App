using Avalonia.Media.Imaging;
using BitFaster.Caching;
using BitFaster.Caching.Lru;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Resources.Caching;
using NexusMods.Abstractions.Resources.DB;
using NexusMods.Abstractions.Resources.IO;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Hashing.xxHash64;
using NexusMods.Media;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using SkiaSharp;

namespace NexusMods.UI.Tests;

public class ImageLoaderTests : AUiTest
{
    public ImageLoaderTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test()
    {
        var library = Provider.GetRequiredService<NexusModsLibrary>();
        var modPage = await library.GetOrAddModPage(ModId.From(6072), Cyberpunk2077Game.StaticDomain);

        {
            var pipeline = CreatePipeline();

            using var lifetime1 = (await pipeline.LoadResourceAsync(modPage, CancellationToken.None)).Data;
            using var lifetime2 = (await pipeline.LoadResourceAsync(modPage, CancellationToken.None)).Data;
            lifetime1.Value.Should().BeSameAs(lifetime2.Value);
            lifetime1.ReferenceCount.Should().Be(1);
            lifetime2.ReferenceCount.Should().Be(2);

            lifetime1.Value.Size.Should().Be(new Avalonia.Size(120, 80));
        }

        {
            var pipeline = CreatePipeline();

            using var lifetime = (await pipeline.LoadResourceAsync(modPage, CancellationToken.None)).Data;
            lifetime.Value.Size.Should().Be(new Avalonia.Size(120, 80));
        }
    }

    private IResourceLoader<EntityId, Lifetime<Bitmap>> CreatePipeline()
    {
        const byte partitionId = 123;

        var pipeline = new HttpLoader(new HttpClient())
            .ChangeIdentifier<ValueTuple<EntityId, Uri>, Uri, byte[]>(static tuple => tuple.Item2)
            .Decode(decoderType: DecoderType.Skia)
            .Resize(newSize: new SKSizeI(
                width: 120,
                height: 80
            ))
            .Encode(encoderType: EncoderType.Qoi)
            .PersistInDb(
                connection: Connection,
                referenceAttribute: NexusModsModPageMetadata.ThumbnailResource,
                identifierToHash: static uri => uri.ToString().XxHash64AsUtf8(),
                partitionId: PartitionId.User(partitionId)
            )
            .Decode(decoderType: DecoderType.Qoi)
            .ToAvaloniaBitmap()
            .UseScopedCache(
                keyGenerator: static tuple => tuple.Item1,
                keyComparer: EqualityComparer<EntityId>.Default,
                capacityPartition: new FavorWarmPartition(totalCapacity: 10, warmRatio: FavorWarmPartition.DefaultWarmRatio)
            )
            .EntityIdToIdentifier(
                connection: Connection,
                attribute: NexusModsModPageMetadata.FullSizedPictureUri
            );

        return pipeline;
    }
}
