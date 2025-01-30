using NexusMods.Abstractions.Resources;
using SkiaSharp;

namespace NexusMods.Media;

public sealed class ImageEncoder<TResourceIdentifier> : ANestedResourceLoader<TResourceIdentifier, byte[], SKBitmap>
    where TResourceIdentifier : notnull
{
    private readonly EncoderType _encoderType;

    public ImageEncoder(
        EncoderType encoderType,
        IResourceLoader<TResourceIdentifier, SKBitmap> innerLoader) : base(innerLoader)
    {
        _encoderType = encoderType;
    }

    protected override ValueTask<Resource<byte[]>> ProcessResourceAsync(
        Resource<SKBitmap> resource,
        TResourceIdentifier resourceIdentifier,
        CancellationToken cancellationToken)
    {
        var skBitmap = resource.Data;

        var encoded = _encoderType switch
        {
            EncoderType.Qoi => ToQoi(),
            EncoderType.SkiaWebp => WithSkia(),
        };

        return ValueTask.FromResult(new Resource<byte[]>
        {
            Data = encoded,
            ExpiresAt = resource.ExpiresAt,
        });

        byte[] WithSkia()
        {
            using var ms = new MemoryStream();

            var skiaFormat = _encoderType switch
            {
                EncoderType.SkiaWebp => SKEncodedImageFormat.Webp,
                EncoderType.Qoi => throw new NotSupportedException(),
            };

            resource.Data.Encode(ms, skiaFormat, quality: 80);
            return ms.ToArray();
        }

        byte[] ToQoi()
        {
            var pixels = PreparePixels(skBitmap);
            var qoiImage = new QoiSharp.QoiImage(
                data: pixels,
                width: skBitmap.Width,
                height: skBitmap.Height,
                channels: QoiSharp.Codec.Channels.RgbWithAlpha,
                colorSpace: QoiSharp.Codec.ColorSpace.SRgb
            );

            return QoiSharp.QoiEncoder.Encode(qoiImage);
        }
    }

    private static byte[] PreparePixels(SKBitmap skBitmap)
    {
        // TODO: check if input is already in the correct layout, then we can skip this transposing step
        var skImageInfo = skBitmap.Info
            .WithAlphaType(SKAlphaType.Unpremul)
            .WithColorType(SKColorType.Rgba8888)
            .WithColorSpace(SKColorSpace.CreateSrgb());

        using var outputSkBitmap = new SKBitmap(skImageInfo);

        using (var skCanvas = new SKCanvas(outputSkBitmap))
        using (var skPaint = new SKPaint())
        {
            skCanvas.DrawBitmap(
                bitmap: skBitmap,
                dest: skImageInfo.Rect,
                paint: skPaint
            );
        }

        return outputSkBitmap.GetPixelSpan().ToArray();
    }
}

public static partial class Extensions
{
    public static IResourceLoader<TResourceIdentifier, byte[]> Encode<TResourceIdentifier>(
        this IResourceLoader<TResourceIdentifier, SKBitmap> inner,
        EncoderType encoderType)
        where TResourceIdentifier : notnull
    {
        return inner.Then(
            state: encoderType,
            factory: static (encoderType, inner) => new ImageEncoder<TResourceIdentifier>(
                encoderType: encoderType,
                innerLoader: inner
            )
        );
    }
}

public enum EncoderType : byte
{
    Qoi = 0,
    SkiaWebp = 1,
}
