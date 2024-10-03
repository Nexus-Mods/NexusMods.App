using NexusMods.Abstractions.Resources;
using SkiaSharp;

namespace NexusMods.Media;

public sealed class ImageResizer<TResourceIdentifier> : ANestedResourceLoader<TResourceIdentifier, SKBitmap, SKBitmap>
    where TResourceIdentifier : notnull
{
    private readonly SKSizeI _newSize;

    public ImageResizer(
        SKSizeI newSize,
        IResourceLoader<TResourceIdentifier, SKBitmap> innerLoader) : base(innerLoader)
    {
        _newSize = newSize;
    }

    protected override ValueTask<Resource<SKBitmap>> ProcessResourceAsync(
        Resource<SKBitmap> resource,
        TResourceIdentifier resourceIdentifier,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(resource with
        {
            Data = resource.Data.Resize(_newSize, quality: SKFilterQuality.Low),
        });
    }
}

public static partial class Extensions
{
    public static IResourceLoader<TResourceIdentifier, SKBitmap> Resize<TResourceIdentifier>(
        this IResourceLoader<TResourceIdentifier, SKBitmap> inner,
        SKSizeI newSize)
        where TResourceIdentifier : notnull
    {
        return inner.Then(
            state: newSize,
            factory: static (newSize, inner) => new ImageResizer<TResourceIdentifier>(
                newSize: newSize,
                innerLoader: inner
            )
        );
    }
}
