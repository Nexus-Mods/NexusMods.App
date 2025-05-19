using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using NexusMods.Sdk.Resources;
using SkiaSharp;

namespace NexusMods.UI.Sdk.Resources;

public sealed class AvaloniaImageLoader<TResourceIdentifier> : ANestedResourceLoader<TResourceIdentifier, Bitmap, SKBitmap>
    where TResourceIdentifier : notnull
{
    public AvaloniaImageLoader(IResourceLoader<TResourceIdentifier, SKBitmap> innerLoader) : base(innerLoader) { }

    protected override ValueTask<Resource<Bitmap>> ProcessResourceAsync(
        Resource<SKBitmap> resource,
        TResourceIdentifier resourceIdentifier,
        CancellationToken cancellationToken)
    {
        var skBitmap = resource.Data;
        var bitmap = ToBitmap(skBitmap);

        return ValueTask.FromResult(new Resource<Bitmap>
        {
            Data = bitmap,
            ExpiresAt = resource.ExpiresAt,
        });
    }

    private static Bitmap ToBitmap(SKBitmap skBitmap)
    {
        var data = skBitmap.GetPixels();
        ArgumentOutOfRangeException.ThrowIfZero(data);

        var bitmap = new Bitmap(
            format: skBitmap.ColorType.ToPixelFormat(),
            alphaFormat: skBitmap.AlphaType.ToAlphaFormat(),
            data: data,
            size: new PixelSize(skBitmap.Width, skBitmap.Height),
            dpi: new Vector(96.0, 96.0),
            stride: skBitmap.RowBytes
        );

        return bitmap;
    }
}

public static partial class Extensions
{
    public static IResourceLoader<TResourceIdentifier, Bitmap> ToAvaloniaBitmap<TResourceIdentifier>(
        this IResourceLoader<TResourceIdentifier, SKBitmap> inner)
        where TResourceIdentifier : notnull
    {
        return inner.Then(
            factory: static inner => new AvaloniaImageLoader<TResourceIdentifier>(
                innerLoader: inner
            )
        );
    }
}
