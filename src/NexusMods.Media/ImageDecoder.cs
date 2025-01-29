using NexusMods.Abstractions.Resources;
using SkiaSharp;

namespace NexusMods.Media;

public sealed class ImageDecoder<TResourceIdentifier> : ANestedResourceLoader<TResourceIdentifier, SKBitmap, byte[]>
    where TResourceIdentifier : notnull
{
    private readonly DecoderType _decoderType;

    public ImageDecoder(
        DecoderType decoderType,
        IResourceLoader<TResourceIdentifier, byte[]> innerLoader
    ) : base(innerLoader)
    {
        _decoderType = decoderType;
    }

    protected override ValueTask<Resource<SKBitmap>> ProcessResourceAsync(
        Resource<byte[]> resource,
        TResourceIdentifier resourceIdentifier,
        CancellationToken cancellationToken)
    {
        var decoded = Decode(resource.Data, _decoderType);
        return ValueTask.FromResult(new Resource<SKBitmap>
        {
            Data = decoded,
            ExpiresAt = resource.ExpiresAt,
        });
    }

    private static SKBitmap Decode(byte[] data, DecoderType format)
    {
        return format switch
        {
            DecoderType.Qoi => DecodeQoi(),
            DecoderType.Skia => SKBitmap.Decode(data),
        };

        unsafe SKBitmap DecodeQoi()
        {
            var qoiImage = QoiSharp.QoiDecoder.Decode(data);

            var skImageInfo = new SKImageInfo(
                width: qoiImage.Width,
                height: qoiImage.Height,
                colorType: SKColorType.Rgba8888,
                alphaType: SKAlphaType.Unpremul,
                colorspace: SKColorSpace.CreateSrgb()
            );

            var skBitmap = new SKBitmap(skImageInfo);

            var pixels = skBitmap.GetPixels(out var length);
            ArgumentOutOfRangeException.ThrowIfZero(pixels);

            var span = new Span<byte>((void*) pixels, (int) length);
            qoiImage.Data.CopyTo(span);

            return skBitmap;
        }
    }
}

public static partial class Extensions
{
    public static IResourceLoader<TResourceIdentifier, SKBitmap> Decode<TResourceIdentifier>(
        this IResourceLoader<TResourceIdentifier, byte[]> inner,
        DecoderType decoderType)
        where TResourceIdentifier : notnull
    {
        return inner.Then(
            state: decoderType,
            factory: static (decoderType, inner) => new ImageDecoder<TResourceIdentifier>(
                decoderType: decoderType,
                innerLoader: inner
            )
        );
    }
}

public enum DecoderType
{
    Qoi = 0,
    Skia = 1,
}
