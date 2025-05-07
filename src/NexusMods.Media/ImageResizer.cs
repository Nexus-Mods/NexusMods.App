using NexusMods.Abstractions.Resources;
using SkiaSharp;

namespace NexusMods.Media;

public sealed class ImageResizer<TResourceIdentifier> : ANestedResourceLoader<TResourceIdentifier, SKBitmap, SKBitmap>
    where TResourceIdentifier : notnull
{
    private readonly SKSizeI _newSize;
    private readonly bool _maintainAspectRatio;

    public ImageResizer(
        SKSizeI newSize,
        IResourceLoader<TResourceIdentifier, SKBitmap> innerLoader,
        bool maintainAspectRatio = true) : base(innerLoader)
    {
        _newSize = newSize;
        _maintainAspectRatio = maintainAspectRatio;
    }

    protected override ValueTask<Resource<SKBitmap>> ProcessResourceAsync(
        Resource<SKBitmap> resource,
        TResourceIdentifier resourceIdentifier,
        CancellationToken cancellationToken)
    {
        var original = resource.Data;

        var targetSize = _newSize;

        /*
         * If Width is larger, the Height is calculated. If Height is larger, the Width is calculated. This ensures the resizing
         * is based on the dominant dimension while maintaining the aspect ratio
        */
        if (_maintainAspectRatio)
        {
            var aspectRatio = (float)original.Height / original.Width;

            if (_newSize.Width > _newSize.Height)
            {
                var newHeight = (int)(_newSize.Width * aspectRatio);
                targetSize = new SKSizeI(_newSize.Width, newHeight);
            }
            else
            {
                var newWidth = (int)(_newSize.Height / aspectRatio);
                targetSize = new SKSizeI(newWidth, _newSize.Height);
            }
        }

        return ValueTask.FromResult(resource with
            {
                // previous Resize method was obsolete
                Data = original.Resize(targetSize, SKSamplingOptions.Default),
            }
        );
    }
}

public static partial class Extensions
{
    public static IResourceLoader<TResourceIdentifier, SKBitmap> Resize<TResourceIdentifier>(
        this IResourceLoader<TResourceIdentifier, SKBitmap> inner,
        SKSizeI newSize,
        bool maintainAspectRatio = true)
        where TResourceIdentifier : notnull
    {
        return inner.Then(
            state: (newSize, maintainAspectRatio),
            factory: static (state, inner) => new ImageResizer<TResourceIdentifier>(
                newSize: state.newSize,
                innerLoader: inner,
                maintainAspectRatio: state.maintainAspectRatio
            )
        );
    }
}
